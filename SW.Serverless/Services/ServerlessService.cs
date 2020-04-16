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
        private const string adaptersNamingPrefix = "serverless.adapters";
        private readonly ServerlessOptions serverlessOptions;
        private readonly IMemoryCache memoryCache;
        private readonly ILoggerFactory loggerFactory;
        private readonly ILogger<ServerlessService> logger;
        private readonly CloudFilesOptions cloudFilesOptions;
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

            logger = loggerFactory.CreateLogger<ServerlessService>();

            cloudFilesOptions = new CloudFilesOptions
            {
                AccessKeyId = serverlessOptions.AccessKeyId,
                BucketName = serverlessOptions.BucketName,
                SecretAccessKey = serverlessOptions.SecretAccessKey,
                ServiceUrl = serverlessOptions.ServiceUrl

            };

        }

        //var adapterpath = @"C:\Users\Samer Awajan\source\repos\Serverless\SW.Serverless.SampleAdapter2\bin\Debug\netcoreapp3.1\SW.Serverless.SampleAdapter2.dll";

        async public Task StartAsync(string adapterId, string[] arguments = null)
        {
            if (string.IsNullOrWhiteSpace(adapterId) || adapterId.Contains(' '))
            {
                throw new ArgumentException("Invalid name.", nameof(adapterId));
            }

            var adapterpath = await Install(adapterId);

            await StartAsync(adapterId, adapterpath, arguments);
        }

        public Task StartAsync(string adapterId, string adapterPath, string[] arguments = null)
        {
            if (processStarted)
                throw new Exception("Already started.");

            adapterLogger = loggerFactory.CreateLogger($"{adaptersNamingPrefix}.{adapterId}".ToLower());

            process = new Process
            {
                StartInfo = new ProcessStartInfo("dotnet")
                {
                    Arguments = $"\"{adapterPath}\" {serverlessOptions.IdleTimeout}",
                    WorkingDirectory = Path.GetDirectoryName(adapterPath),
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
                throw new Exception("Process reused!");

            processStarted = true;

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            return Task.CompletedTask;
        }

        async public Task InvokeVoidAsync(string command, string input = null)
        {
            await InvokeAsync(command, serverlessOptions.CommandTimeout, input);
        }

        async public Task InvokeVoidAsync(string command, int commandTimeout, string input = null)
        {
            await InvokeAsync(command, commandTimeout, input);
        }

        async public Task<string> InvokeAsync(string command, string input = null)
        {
            return await InvokeAsync(command, serverlessOptions.CommandTimeout, input);
        }
        async public Task<string> InvokeAsync(string command, int commandTimeout, string input = null)
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
                dueTime: TimeSpan.FromSeconds(commandTimeout),
                period: Timeout.InfiniteTimeSpan);

            if (input == null) input = Constants.NullIdentifier;

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
                //adapterLogger.LogWarning("Null data received on error stream.");
            }
            else if (args.Data.StartsWith(Constants.LogInformationIdentifier))
            {
                adapterLogger.LogInformation(args.Data.Replace(Constants.LogInformationIdentifier, "").Replace(Constants.NewLineIdentifier, "\n"));
            }
            else if (args.Data.StartsWith(Constants.LogWarningIdentifier))
            {
                adapterLogger.LogWarning(args.Data.Replace(Constants.LogWarningIdentifier, "").Replace(Constants.NewLineIdentifier, "\n"));
            }
            else if (args.Data.StartsWith(Constants.LogErrorIdentifier))
            {
                adapterLogger.LogError(args.Data.Replace(Constants.LogErrorIdentifier, "").Replace(Constants.NewLineIdentifier, "\n"));
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
            var adapterDiretoryPath = $"{serverlessOptions.AdapterLocalPath}/{adapterConfig.Hash}";
            var adapterPath = Path.GetFullPath($"{adapterDiretoryPath}/{adapterConfig.EntryAssembly}");

            await semaphoreSlim.WaitAsync();
            try
            {
                if (!Directory.Exists(adapterDiretoryPath))
                {
                    Directory.CreateDirectory(adapterDiretoryPath);
                    try
                    {
                        using var cloudFilesService = new CloudFilesService(cloudFilesOptions);
                        using var stream = await cloudFilesService.OpenReadAsync($"{serverlessOptions.AdapterRemotePath}/{adapterId}".ToLower());
                        using var archive = new ZipArchive(stream);

                        foreach (var entry in archive.Entries)
                            entry.ExtractToFile($"{adapterDiretoryPath}/{entry.Name}");

                        //Process.Start("chmod", $"755 {adapterPath}").WaitForExit(5000);
                    }
                    catch (Exception)
                    {
                        Directory.Delete(adapterDiretoryPath, true);
                        throw;
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
            if (memoryCache.TryGetValue($"{adaptersNamingPrefix}.{adapterId}", out AdapterMetadata adapterMetadata))
                return adapterMetadata;

            using var cloudFilesService = new CloudFilesService(cloudFilesOptions);

            var metaData = await cloudFilesService.GetMetadataAsync($"{serverlessOptions.AdapterRemotePath}/{adapterId}".ToLower());
            adapterMetadata = new AdapterMetadata
            {
                EntryAssembly = metaData["EntryAssembly"],
                Hash = metaData["Hash"]
            };

            return memoryCache.Set($"{adaptersNamingPrefix}.{adapterId}", adapterMetadata, TimeSpan.FromMinutes(serverlessOptions.AdapterMetadataCacheDuration));
        }


        public void Dispose()
        {
            try
            {
                if (processStarted)
                {

                    if (!process.HasExited)
                    {
                        process.StandardInput.WriteLine(Constants.QuitCommand);
                        process.WaitForExit(3000);
                        if (!process.HasExited) process.Kill();
                    }

                    process.Dispose();

                    invocationTimeoutTimer?.Dispose();
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Service did not dispose properly.");
            }

        }
    }
}
