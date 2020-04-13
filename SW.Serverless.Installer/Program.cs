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
            //foreach (var err in errs)
            //{
            //    if (err is MissingRequiredOptionError error)
            //        Console.WriteLine($"Missing required option {error.NameInfo.NameText}.");


            //}

            Console.ReadKey();
        }

        static void RunOptions(Options opts)
        {

            try
            {
                var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
                var process = new Process
                {
                    EnableRaisingEvents = true,
                    StartInfo = new ProcessStartInfo("dotnet")
                    {
                        Arguments = $"publish \"{opts.ProjectPath}\" -o \"{tempPath}\"",
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
                process.WaitForExit();

                var filesToCompress = Directory.GetFiles(tempPath);
                var zipFileName = Path.Combine(tempPath, $"{opts.AdapterName}");

                {
                    using var stream = File.OpenWrite(zipFileName);
                    using var archive = new ZipArchive(stream, ZipArchiveMode.Create);

                    foreach (var file in filesToCompress)
                        archive.CreateEntryFromFile(file, Path.GetFileName(file));
                }

                {
                    var projectFileName = Path.GetFileName(opts.ProjectPath);
                    var entryAssembly = $"{projectFileName.Remove(projectFileName.LastIndexOf('.'))}.dll";

                    using var cloudService = new CloudFilesService(new CloudFilesOptions
                    {
                        AccessKeyId = opts.AccessKeyId,
                        SecretAccessKey = opts.SecretAccessKey,
                        ServiceUrl = opts.ServiceUrl,
                        BucketName = opts.BucketName
                    });

                    using var zipFileStream = File.OpenRead(zipFileName);

                    cloudService.WriteAsync(zipFileStream, new WriteFileSettings
                    {
                        ContentType = "application/zip",
                        Key = $"adapters/{opts.AdapterName}".ToLower(),
                        Metadata = new Dictionary<string, string>
                        {
                            {"EntryAssembly", entryAssembly}
                        }
                    }).Wait();
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            Console.ReadLine();

        }


        static void OutputDataReceived(object sender, DataReceivedEventArgs args)
        {
            Console.WriteLine(args.Data);
        }
    }
}
