using CommandLine;
using SW.Serverless.Installer.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SW.CloudFiles.OC;
using SW.PrimitiveTypes;

namespace SW.Serverless.Installer
{
    class Program
    {
        private static async Task Main(string[] args)
        {
            var parser = Parser.Default.ParseArguments<CliOptions>(args);
            await parser
                .WithParsedAsync(RunOptions)
                .Result
                .WithNotParsedAsync(HandleParseError);
        }

        private static Task HandleParseError(IEnumerable<Error> arg)
        {
            return Task.CompletedTask;
        }


        private static async Task<ServerlessUploadOptions> GetServerlessUploadOptions(CliOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.CloudFilesConfigPath))
                return new ServerlessUploadOptions
                {
                    AccessKeyId = options.AccessKeyId,
                    SecretAccessKey = options.SecretAccessKey,
                    ServiceUrl = options.ServiceUrl,
                    BucketName = options.BucketName,
                    Version = options.Version,
                    Provider = options.Provider,
                    AdapterId = options.AdapterId,
                };


            var rawFile = await File.ReadAllTextAsync(options.CloudFilesConfigPath);

            if (string.IsNullOrWhiteSpace(rawFile))
                throw new SWException($"Invalid cloud Files config path, {options.CloudFilesConfigPath}");


            var data = JsonConvert.DeserializeObject<OracleCloudFilesOptions>(rawFile);
            return new ServerlessUploadOptions
            {
                Provider = options.Provider,
                AccessKeyId = data.AccessKeyId,
                SecretAccessKey = data.SecretAccessKey,
                ServiceUrl = data.ServiceUrl,
                BucketName = data.BucketName,
                Region = data.Region,
                FingerPrint = data.FingerPrint,
                TenantId = data.TenantId,
                UserId = data.UserId,
                RSAKey = data.Region,
                Version = options.Version,
                AdapterId = options.AdapterId,
            };
        }

        static async Task RunOptions(CliOptions opts)
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
                var entryAssembly = $"{projectFileName!.Remove(projectFileName.LastIndexOf('.'))}.dll";

                if (!await installer.PushToCloud(zipFileName,entryAssembly, opts)) return;

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