using SW.CloudFiles;
using SW.PrimitiveTypes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace SW.Serverless.Installer
{
    class Program
    {
        async static Task Main(string[] args)
        {

            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

            var adapterName = args[1];

            var process = new Process
            {
                EnableRaisingEvents = true,
                StartInfo = new ProcessStartInfo("dotnet")
                {

                    Arguments = $"publish \"{args[0]}\" -o \"{tempPath}\"",
                    //WorkingDirectory = Path.GetDirectoryName(adapterpath),
                    UseShellExecute = false,
                    //RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,

                }


            };

            process.OutputDataReceived += OutputDataReceived;
            process.ErrorDataReceived += OutputDataReceived;

            //
            process.Start();
            process.BeginOutputReadLine();
            process.WaitForExit();

            var filesToCompress = Directory.GetFiles(tempPath);
            var zipFileName = Path.Combine(tempPath, $"{args[1]}");

            {


                using var stream = File.OpenWrite(zipFileName);
                using var archive = new ZipArchive(stream, ZipArchiveMode.Create);

                foreach (var file in filesToCompress)
                    archive.CreateEntryFromFile(file, Path.GetFileName(file));
            }

            {
                var projectFileName = Path.GetFileName(args[0]);
                var entryAssembly = $"{projectFileName.Remove(projectFileName.LastIndexOf('.'))}.dll";

                using var cloudService = new CloudFilesService(new CloudFilesOptions
                {
                    AccessKeyId = "R3LNFRKWMAC4OCCRICS5",
                    SecretAccessKey = "YPyyTdxs+lZMQEtYIDRK9lkIzjJrCKXinE3OfKEfc7k",
                    ServiceUrl = "https://fra1.digitaloceanspaces.com",
                    BucketName = "sf9"
                });

                using var zipFileStream = File.OpenRead(zipFileName);

                await cloudService.WriteAcync(zipFileStream, new WriteFileSettings
                {
                    ContentType = "application/zip",
                    Key = $"adapters/{args[1]}".ToLower(),
                    Metadata = new Dictionary<string, string>
                    {
                        {"EntryAssembly", entryAssembly}
                    }
                });

            }


            Console.ReadLine();
        }

        //static void ErrorDataReceived(object sender, DataReceivedEventArgs args)
        //{
        //}

        static void OutputDataReceived(object sender, DataReceivedEventArgs args)
        {
            Console.WriteLine(args.Data);
        }
    }
}
