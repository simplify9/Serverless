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
        private static async Task Main(string[] args)
        {
            var parser = Parser.Default.ParseArguments<Options>(args);
            await parser
                .WithParsedAsync(RunOptions)
                .Result
                .WithNotParsedAsync(HandleParseError);
        }

        private static Task HandleParseError(IEnumerable<Error> arg)
        {
            return Task.CompletedTask;
        }


        static async Task RunOptions(Options opts)
        {
            var installer = new InstallerLogic();
            try
            {
                Environment.ExitCode = 1;

                var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

                if (!InstallerLogic.BuildPublish(opts.ProjectPath, tempPath)) return;

                var zipFileName = Path.Combine(tempPath, $"{opts.AdapterId}");

                if (!InstallerLogic.Compress(tempPath, zipFileName)) return;

                var projectFileName = Path.GetFileName(opts.ProjectPath);
                var entryAssembly = $"{projectFileName!.Remove(projectFileName.LastIndexOf('.'))}.dll";

                if (!await InstallerLogic.PushToCloud(zipFileName,entryAssembly, opts)) return;

                if (!InstallerLogic.Cleanup(tempPath)) return;

                Environment.ExitCode = 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}