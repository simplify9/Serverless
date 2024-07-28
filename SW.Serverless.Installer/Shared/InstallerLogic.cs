using SW.PrimitiveTypes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Azure.Storage;
using Azure.Storage.Blobs;
using SW.CloudFiles.OC;
using CloudFilesService = SW.CloudFiles.AS.CloudFilesService;

namespace SW.Serverless.Installer.Shared
{
    public class InstallerLogic
    {
        public bool BuildPublish(string projectPath, string outputPath)
        {
            Console.WriteLine("Building and publishing...");

            var process = new Process
            {
                EnableRaisingEvents = true,
                StartInfo = new ProcessStartInfo("dotnet")
                {
                    Arguments = $"publish \"{projectPath}\" -o \"{outputPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                }
            };

            process.OutputDataReceived += OutputDataReceived;
            process.ErrorDataReceived += OutputDataReceived;
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            var result = process.ExitCode == 0;

            Console.WriteLine($"Building and publishing {(result ? "succeeded" : "failed")}.");

            return result;
        }

        public bool Compress(string path, string zipFileName)
        {
            try
            {
                Console.WriteLine("Compressing files...");
                var filesToCompress = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
                {
                    using var stream = File.OpenWrite(zipFileName);
                    using var archive = new ZipArchive(stream, ZipArchiveMode.Create);

                    foreach (var file in filesToCompress)
                    {
                        var entryName = Path.GetRelativePath(path, file);
                        archive.CreateEntryFromFile(file, entryName);
                    }
                }
                Console.WriteLine("Compressing files succeeded.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Compressing files failed: {ex}");
                return false;
            }
        }

        private static async Task<CloudFilesService> GetAzureStorageConfig(CloudFilesOptions cloudFilesOptions)
        {
            var blobContainerClient = new BlobServiceClient(
                new Uri(cloudFilesOptions.ServiceUrl),
                new StorageSharedKeyCredential(cloudFilesOptions.AccessKeyId,
                    cloudFilesOptions.SecretAccessKey)).GetBlobContainerClient(cloudFilesOptions.BucketName);

            return await blobContainerClient.ExistsAsync()
                ? new CloudFilesService(blobContainerClient)
                : new CloudFilesService(
                    await new BlobServiceClient(new Uri(cloudFilesOptions.ServiceUrl),
                            new StorageSharedKeyCredential(cloudFilesOptions.AccessKeyId,
                                cloudFilesOptions.SecretAccessKey))
                        .CreateBlobContainerAsync(cloudFilesOptions.BucketName));
        }

        private static Task<CloudFiles.S3.CloudFilesService> GetS3Config(CloudFilesOptions cloudFilesOptions)
        {
            return Task.FromResult(new CloudFiles.S3.CloudFilesService(cloudFilesOptions));
        }

        private static Task<CloudFiles.OC.CloudFilesService> GetOracleCloudConfigConfig(
            Options options)
        {
            var config = new OracleCloudFilesOptions();
            var directory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            var pemPath = Path.Combine(directory, $"{Guid.NewGuid():N}.pem");
            File.WriteAllText(pemPath, options.RSAKey);
            var configPAth = Path.Combine(directory, $"{Guid.NewGuid():N}.config");
            File.WriteAllText(configPAth, @$"[DEFAULT]
user={options.UserId}
fingerprint={options.FingerPrint}
tenancy={options.TenantId}
region={options.Region}
key_file={pemPath}");
            config.ConfigPath = configPAth;
            return Task.FromResult(new CloudFiles.OC.CloudFilesService(config, null));
        }

        private static async Task UploadVersioned(ICloudFilesService cloudService, Stream zipFileStream,
            string adapterId,
            string entryAssembly, string version)
        {
            var dir = $"adapters/{adapterId}".ToLower();
            var list = (await cloudService.ListAsync(dir)).ToList();
            var fileName = Semver.GetNewVersion(version, list.Select(i => i.Key.Split("/").Last()).ToList());
            var path = $"{dir}/{fileName}";
            Console.WriteLine($"Uploading to {path} Versioned");
            await cloudService.WriteAsync(zipFileStream, new WriteFileSettings
            {
                ContentType = "application/zip",
                Key = path,
                Metadata = new Dictionary<string, string>
                {
                    { "EntryAssembly", entryAssembly },
                    { "Lang", "dotnet" },
                    { "Timestamp", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ") }
                }
            });
        }

        private static async Task UploadLegacy(ICloudFilesService cloudService, Stream zipFileStream, string adapterId,
            string entryAssembly)
        {
            var path = $"adapters/{adapterId}".ToLower();
            Console.WriteLine($"Uploading to {path}");
            await cloudService.WriteAsync(zipFileStream, new WriteFileSettings
            {
                ContentType = "application/zip",
                Key = path,
                Metadata = new Dictionary<string, string>
                {
                    { "EntryAssembly", entryAssembly },
                    { "Lang", "dotnet" },
                    { "Timestamp", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ") }
                }
            });
        }

        public async Task<bool> PushToCloud(
            string zipFilePath,
            string entryAssembly,
            Options options)
        {
            try
            {
                Console.WriteLine("Starting...");
                var cloudFilesOptions = new CloudFilesOptions
                {
                    AccessKeyId = options.AccessKeyId,
                    SecretAccessKey = options.SecretAccessKey,
                    ServiceUrl = options.ServiceUrl,
                    BucketName = options.BucketName
                };
                ICloudFilesService cloudService = options.Provider?.ToLower() switch
                {
                    "as" => await GetAzureStorageConfig(cloudFilesOptions),
                    "s3" => await GetS3Config(cloudFilesOptions),
                    "oc" => await GetOracleCloudConfigConfig(options),
                    _ => await GetS3Config(cloudFilesOptions),
                };
                Console.WriteLine("Reading file...");

                await using var zipFileStream = File.OpenRead(zipFilePath);
                Console.WriteLine("Pushing to cloud...");

                if (string.IsNullOrWhiteSpace(options.Version))
                {
                    await UploadLegacy(cloudService, zipFileStream, options.AdapterId, entryAssembly);
                }
                else
                {
                    await UploadVersioned(cloudService, zipFileStream, options.AdapterId, entryAssembly,
                        options.Version);
                }


                if (options.Provider?.ToLower() != "as") ((CloudFiles.S3.CloudFilesService)cloudService)?.Dispose();
                Console.WriteLine("Pushing to cloud succeeded.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Pushing to cloud failed: {ex}");
                return false;
            }
        }

        public bool Cleanup(string tempPath)
        {
            try
            {
                Console.WriteLine("Cleaning up...");
                Directory.Delete(tempPath, true);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Cleaning up failed: {ex}");
                return false;
            }
        }

        private static void OutputDataReceived(object sender, DataReceivedEventArgs args)
        {
            Console.WriteLine(args.Data);
        }
    }
}