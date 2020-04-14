using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SW.CloudFiles;
using SW.PrimitiveTypes;
using SW.Serverless.Sdk;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SW.Serverless
{
    public class ServerlessService : IServerlessService, IDisposable
    {
        private static readonly SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);
        private const string serverlessAdaptersPrefix = "serverless.adapters";
        private readonly ServerlessOptions serverlessOptions;
        private readonly IMemoryCache memoryCache;
        private readonly ILoggerFactory loggerFactory;
        private Process process;
        private TaskCompletionSource<string> taskCompletionSource;
        private Timer invocationTimeoutTimer;
        private bool processStarted;
        private ILogger adapterLogger;


        public ServerlessService(ServerlessOptions serverlessOptions, IMemoryCache memoryCache, ILoggerFactory loggerFactory)
        {
            this.serverlessOptions = serverlessOptions;
            this.memoryCache = memoryCache;
            this.loggerFactory = loggerFactory;
            
        }

        async public Task StartAsync(string adapterId, string[] arguments = null)
        {
            if (string.IsNullOrWhiteSpace(adapterId) || adapterId.Contains(' '))
            {
                throw new ArgumentException("Invalid name.", nameof(adapterId));
            }

            if (processStarted)
                throw new Exception("Already started.");

            //var adapterpath = await Install(adapterId);
            var adapterpath = @"C:\Users\Samer Awajan\source\repos\Serverless\SW.Serverless.SampleAdapter2\bin\Debug\netcoreapp3.1\SW.Serverless.SampleAdapter2.dll";

            adapterLogger = loggerFactory.CreateLogger($"{serverlessAdaptersPrefix}.{adapterId}".ToLower());

            process = new Process
            {
                EnableRaisingEvents = true,
                StartInfo = new ProcessStartInfo("dotnet")
                {
                    Arguments = $"\"{adapterpath}\" testarg1 \"testagr2\"",
                    WorkingDirectory = Path.GetDirectoryName(adapterpath),
                    UseShellExecute = false,

                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,

                    StandardInputEncoding = Encoding.UTF8,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8,
                }
            };

            process.OutputDataReceived += OutputDataReceived;
            process.ErrorDataReceived += ErrorDataReceived;

            if (!process.Start())
                throw new SWException("Process reused!");

            processStarted = true;

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }

        async public Task InvokeVoidAsync(string command, string input = null)
        {
            await InvokeAsync(command, input);
        }

        async public Task<string> InvokeAsync(string command, string input = null)
        {

            if (string.IsNullOrWhiteSpace(command) || command.Contains(' '))
            {
                throw new ArgumentException("Invalid name.", nameof(command));
            }

            if (!processStarted || process.HasExited)
                throw new Exception("Process not started or terminated.");

            taskCompletionSource = new TaskCompletionSource<string>();

            invocationTimeoutTimer = new Timer(
                callback: InvocationTimeoutTimerCallback,
                state: null,
                dueTime: TimeSpan.FromSeconds(serverlessOptions.CommandTimeout),
                period: Timeout.InfiniteTimeSpan);


            await process.StandardInput.WriteLineAsync($"{Constants.Delimiter}{command}{Constants.Delimiter}{input}{Constants.Delimiter}".Replace("\n", Constants.NewLineIdentifier));

            return await taskCompletionSource.Task;
        }

        void InvocationTimeoutTimerCallback(object state)
        {
            invocationTimeoutTimer.Dispose();
            taskCompletionSource.TrySetException(new TimeoutException());
        }

        void ErrorDataReceived(object sender, DataReceivedEventArgs args)
        {
            if (args.Data == null)
            {
                adapterLogger.LogWarning("Null data received on error stream.");
            }
            else if (args.Data.StartsWith(Constants.LogInformationIdentifier))
            {
                adapterLogger.LogInformation(args.Data.Replace(Constants.LogInformationIdentifier, "").Replace(Constants.NewLineIdentifier, "\n"));
            }
            else if (args.Data.StartsWith(Constants.LogWarningIdentifier))
            {
                adapterLogger.LogInformation(args.Data.Replace(Constants.LogWarningIdentifier, "").Replace(Constants.NewLineIdentifier, "\n"));
            }
            else if (args.Data.StartsWith(Constants.LogErrorIdentifier))
            {
                adapterLogger.LogInformation(args.Data.Replace(Constants.LogErrorIdentifier, "").Replace(Constants.NewLineIdentifier, "\n"));
            }

        }

        void OutputDataReceived(object sender, DataReceivedEventArgs args)
        {
            invocationTimeoutTimer?.Dispose();

            if (args.Data == null)
            {
                taskCompletionSource?.TrySetException(new Exception("Received null data."));
                return;
            }

            if (args.Data.StartsWith(Constants.ErrorIdentifier))
            {
                taskCompletionSource?.TrySetException(new Exception(args.Data));
                return;
            }

            var outputSegments = args.Data.Split(Constants.Delimiter);

            if (outputSegments.Length != 3)
            {
                taskCompletionSource?.TrySetException(new Exception("Wrong data format."));
                return;
            }

            var outputDenormalized = outputSegments[1].Replace(Constants.NewLineIdentifier, "\n");

            if (outputDenormalized == Constants.NullIdentifier)
                taskCompletionSource.TrySetResult(null);
            else
                taskCompletionSource.TrySetResult(outputDenormalized);
        }

        async Task<string> Install(string adapterId)
        {

            var adapterConfig = await GetAdapterMetadata(adapterId);

            var adapterDiretoryPath = $"{serverlessOptions.AdapterLocalPath}/{adapterConfig.Hash}";//Path.Combine(ConfigurationManager.AppSettings["PluginsFolderPath"], BitConverter.ToString(_data).Replace("-", "").ToLower());
            var adapterPath = Path.GetFullPath($"{adapterDiretoryPath}/{adapterConfig.EntryAssembly}");

            await semaphoreSlim.WaitAsync();
            try
            {
                if (!Directory.Exists(adapterDiretoryPath))
                {
                    Directory.CreateDirectory(adapterDiretoryPath);
                    try
                    {
                        //logger.LogInformation($"Adapter not installed, installing adapter: '{adapterPath}'");

                        using var cloudFilesService = new CloudFilesService(new CloudFilesOptions
                        {
                            AccessKeyId = serverlessOptions.AccessKeyId,
                            BucketName = serverlessOptions.BucketName,
                            SecretAccessKey = serverlessOptions.SecretAccessKey,
                            ServiceUrl = serverlessOptions.ServiceUrl

                        });

                        using var stream = await cloudFilesService.OpenReadAsync($"{serverlessOptions.AdapterRemotePath}/{adapterId}".ToLower());
                        using var archive = new ZipArchive(stream);

                        foreach (var entry in archive.Entries)
                            entry.ExtractToFile($"{adapterDiretoryPath}/{entry.Name}");

                        //Process.Start("chmod", $"755 {adapterPath}").WaitForExit(5000);
                    }
                    catch (Exception ex)
                    {
                        //logger.LogError(ex, $"Failed to install adapter: '{adapterPath}'");
                        Directory.Delete(adapterDiretoryPath, true);
                        throw ex;
                    }
                }
            }
            finally
            {
                semaphoreSlim.Release();
            }

            return adapterPath;
        }

        async Task<AdapterMetadata> GetAdapterMetadata(string adapterId)
        {
            if (memoryCache.TryGetValue($"adapters:{adapterId}", out AdapterMetadata adapterMetadata))
                return adapterMetadata;

            using var cloudFilesService = new CloudFilesService(new CloudFilesOptions
            {
                AccessKeyId = serverlessOptions.AccessKeyId,
                BucketName = serverlessOptions.BucketName,
                SecretAccessKey = serverlessOptions.SecretAccessKey,
                ServiceUrl = serverlessOptions.ServiceUrl

            });

            var metaData = await cloudFilesService.GetMetadataAsync($"{serverlessOptions.AdapterRemotePath}/{adapterId}".ToLower());
            adapterMetadata = new AdapterMetadata
            {
                EntryAssembly = metaData["EntryAssembly"],
                Hash = metaData["Hash"]
            };

            return memoryCache.Set($"adapters:{adapterId}", adapterMetadata, TimeSpan.FromMinutes(serverlessOptions.AdapterMetadataCacheDuration));
        }


        public void Dispose()
        {
            if (processStarted)
            {
                if (!process.HasExited)
                    process.Kill();

                process.Dispose();

                invocationTimeoutTimer?.Dispose();
            }
        }
    }
}
