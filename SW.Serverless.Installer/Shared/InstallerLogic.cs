using SW.CloudFiles;
using SW.PrimitiveTypes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using SW.CloudFiles.OC;


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

        public async Task<bool> PushToCloudAsync(
            string zipFielPath,
            string adapterId,
            string entryAssembly,
            string provider,
            string accessKeyId,
            string secretAccessKey,
            string serviceUrl,
            string bucketName)
        {
            try
            {
                Console.WriteLine("Pushing to cloud...");


                var cloudFilesOptions = new CloudFilesOptions
                {
                    AccessKeyId = accessKeyId,
                    SecretAccessKey = secretAccessKey,
                    ServiceUrl = serviceUrl,
                    BucketName = bucketName
                };


                var isAzureStorage = provider?.ToLower() == "as";
                ICloudFilesService cloudService = null;

                if (isAzureStorage)
                {
                    BlobContainerClient blobContainerClient = new BlobServiceClient(
                        new Uri(cloudFilesOptions.ServiceUrl),
                        new StorageSharedKeyCredential(cloudFilesOptions.AccessKeyId,
                            cloudFilesOptions.SecretAccessKey)).GetBlobContainerClient(cloudFilesOptions.BucketName);

                    cloudService = await blobContainerClient.ExistsAsync()
                        ? new CloudFiles.AS.CloudFilesService(blobContainerClient)
                        : new CloudFiles.AS.CloudFilesService(
                            await new BlobServiceClient(new Uri(cloudFilesOptions.ServiceUrl),
                                    new StorageSharedKeyCredential(cloudFilesOptions.AccessKeyId,
                                        cloudFilesOptions.SecretAccessKey))
                                .CreateBlobContainerAsync(cloudFilesOptions.BucketName));
                }
                else
                    cloudService = new CloudFiles.S3.CloudFilesService(cloudFilesOptions);

                using var zipFileStream = File.OpenRead(zipFielPath);

                await cloudService.WriteAsync(zipFileStream, new WriteFileSettings
                {
                    ContentType = "application/zip",
                    Key = $"adapters/{adapterId}".ToLower(),
                    Metadata = new Dictionary<string, string>
                    {
                        { "EntryAssembly", entryAssembly },
                        { "Lang", "dotnet" },
                        { "Timestamp", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ") }
                    }
                });
                if (!isAzureStorage) (cloudService as CloudFiles.S3.CloudFilesService)?.Dispose();
                Console.WriteLine("Pushing to cloud succeeded.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Pushing to cloud failed: {ex}");
                return false;
            }
        }

        public bool PushToCloud(
            string zipFielPath,
            string adapterId,
            string entryAssembly,
            string provider,
            string accessKeyId,
            string secretAccessKey,
            string serviceUrl,
            string bucketName)
        {
            try
            {
                Console.WriteLine("Pushing to cloud...");


                var cloudFilesOptions = new CloudFilesOptions
                {
                    AccessKeyId = accessKeyId,
                    SecretAccessKey = secretAccessKey,
                    ServiceUrl = serviceUrl,
                    BucketName = bucketName
                };

                var isAzureStorage = provider?.ToLower() == "as";
                ICloudFilesService cloudService = null;

                switch (provider?.ToLower())
                {
                    case "as":
                    {
                        BlobContainerClient blobContainerClient = new BlobServiceClient(
                                new Uri(cloudFilesOptions.ServiceUrl),
                                new StorageSharedKeyCredential(cloudFilesOptions.AccessKeyId,
                                    cloudFilesOptions.SecretAccessKey))
                            .GetBlobContainerClient(cloudFilesOptions.BucketName);

                        cloudService = blobContainerClient.Exists()
                            ? new CloudFiles.AS.CloudFilesService(blobContainerClient)
                            : new CloudFiles.AS.CloudFilesService(
                                new BlobServiceClient(new Uri(cloudFilesOptions.ServiceUrl),
                                        new StorageSharedKeyCredential(cloudFilesOptions.AccessKeyId,
                                            cloudFilesOptions.SecretAccessKey))
                                    .CreateBlobContainer(cloudFilesOptions.BucketName));
                        break;
                    }
                    case "oc":
                    {
                        var ocloudFilesOptions = new OracleCloudFilesOptions();
                        var directory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                        var pemPath = Path.Combine(directory, $"{Guid.NewGuid():N}.pem");
                        File.WriteAllText(pemPath, ocloudFilesOptions.RSAKey);
                        var configPAth = Path.Combine(directory, $"{Guid.NewGuid():N}.config");
                        File.WriteAllText(configPAth, @$"[DEFAULT]
user={ocloudFilesOptions.UserId}
fingerprint={ocloudFilesOptions.FingerPrint}
tenancy={ocloudFilesOptions.TenantId}
region={ocloudFilesOptions.Region}
key_file={pemPath}");
                        ocloudFilesOptions.ConfigPath = configPAth;
                        cloudService = new CloudFilesService(ocloudFilesOptions, null);

                        break;
                    }
                    default:
                    {
                        cloudService = new CloudFiles.S3.CloudFilesService(cloudFilesOptions);
                        break;
                    }
                }


                using var zipFileStream = File.OpenRead(zipFielPath);


                cloudService.WriteAsync(zipFileStream, new WriteFileSettings
                {
                    ContentType = "application/zip",
                    Key = $"adapters/{adapterId}".ToLower(),
                    Metadata = new Dictionary<string, string>
                    {
                        { "EntryAssembly", entryAssembly },
                        { "Lang", "dotnet" },
                        { "Timestamp", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ") }
                    }
                }).Wait();

                if (!isAzureStorage) (cloudService as CloudFiles.S3.CloudFilesService)?.Dispose();

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

        public void OutputDataReceived(object sender, DataReceivedEventArgs args)
        {
            Console.WriteLine(args.Data);
        }
    }
}