using CommandLine;
using SW.CloudFiles;
using SW.PrimitiveTypes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Threading.Tasks;

namespace SW.Serverless.Installer
{
    class Program
    {
        static void Main(string[] args)
        {
            CommandLine.Parser.Default.ParseArguments<Options>(args)
              .WithParsed(RunOptions)
              .WithNotParsed(HandleParseError);
        }

        static void HandleParseError(IEnumerable<Error> errs)
        {
            //Console.ReadKey();
        }

        static void RunOptions(Options opts)
        {

            try
            {
                Environment.ExitCode = 1;

                var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

                if (!BuildPublish(opts.ProjectPath, tempPath)) return;

                var zipFileName = Path.Combine(tempPath, $"{opts.AdapterId}");

                if (!Compress(tempPath, zipFileName)) return;

                var projectFileName = Path.GetFileName(opts.ProjectPath);
                var entryAssembly = $"{projectFileName.Remove(projectFileName.LastIndexOf('.'))}.dll";

                if (!PushToCloud(zipFileName, opts.AdapterId, entryAssembly, opts.AccessKeyId, opts.SecretAccessKey, opts.ServiceUrl, opts.BucketName)) return;

                if (!Cleanup(tempPath)) return;

                Environment.ExitCode = 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

        }

        static bool BuildPublish(string projectPath, string outputPath)
        {
            Console.WriteLine("Building and publishing...");

            var process = new Process
            {
                EnableRaisingEvents = true,
                StartInfo = new ProcessStartInfo("dotnet")
                {
                    Arguments = $"publish \"{projectPath}\" -o \"{outputPath}\"",
                    //WorkingDirectory = Path.GetDirectoryName(adapterpath),
                    //UseShellExecute = false,
                    //RedirectStandardInput = true,
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

        static bool Compress(string path, string zipFileName)
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

        static bool PushToCloud(
            string zipFielPath,
            string adapterId,
            string entryAssembly,
            string accessKeyId,
            string secretAccessKey,
            string serviceUrl,
            string bucketName)
        {

            try
            {

                Console.WriteLine("Pushing to cloud...");


                using var cloudService = new CloudFilesService(new CloudFilesOptions
                {
                    AccessKeyId = accessKeyId,
                    SecretAccessKey = secretAccessKey,
                    ServiceUrl = serviceUrl,
                    BucketName = bucketName
                });

                using var zipFileStream = File.OpenRead(zipFielPath);

                cloudService.WriteAsync(zipFileStream, new WriteFileSettings
                {
                    ContentType = "application/zip",
                    Key = $"adapters/{adapterId}".ToLower(),
                    Metadata = new Dictionary<string, string>
                        {
                            {"EntryAssembly", entryAssembly},
                            {"Lang", "dotnet" }
                        }
                }).Wait();

                Console.WriteLine("Pushing to cloud succeeded.");
                return true;

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Pushing to cloud failed: {ex}");
                return false;
            }
        }

        static bool Cleanup(string tempPath)
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

        static void OutputDataReceived(object sender, DataReceivedEventArgs args)
        {
            Console.WriteLine(args.Data);
        }
    }
}
