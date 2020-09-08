using CommandLine;
using SW.CloudFiles;
using SW.PrimitiveTypes;
using SW.Serverless.Installer.Shared;
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

            var installer = new InstallerLogic();
            try
            {
                Environment.ExitCode = 1;

                var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

                if (!installer.BuildPublish(opts.ProjectPath, tempPath)) return;

                var zipFileName = Path.Combine(tempPath, $"{opts.AdapterId}");

                if (!installer.Compress(tempPath, zipFileName)) return;

                var projectFileName = Path.GetFileName(opts.ProjectPath);
                var entryAssembly = $"{projectFileName.Remove(projectFileName.LastIndexOf('.'))}.dll";

                if (!installer.PushToCloud(zipFileName, opts.AdapterId, entryAssembly, opts.AccessKeyId, opts.SecretAccessKey, opts.ServiceUrl, opts.BucketName)) return;

                if (!installer.Cleanup(tempPath)) return;

                Environment.ExitCode = 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

        }
    }
}
