using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SW.CloudFiles;
using SW.PrimitiveTypes;
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
     class ServerlessServiceOld : IServerlessService
    {
        private static readonly SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);
        private readonly ILogger<ServerlessServiceOld> logger;
        private readonly ServerlessOptions serverlessOptions;
        private readonly IMemoryCache memoryCache;

        //readonly AdapterService adapterService;

        public ServerlessServiceOld(ILogger<ServerlessServiceOld> logger, ServerlessOptions serverlessOptions, IMemoryCache memoryCache)
        {
            this.logger = logger;
            this.serverlessOptions = serverlessOptions;
            this.memoryCache = memoryCache;
            //this.adapterService = adapterService;
        }



        public async Task<string> Run(string adapterId, string input)
        {
            //var adapterpath = await adapterService.Install(adapterId);
            var adapterpath = @"C:\Users\Samer Awajan\source\repos\Serverless\SW.Serverless.SampleAdapter1\bin\Debug\netcoreapp3.1\SW.Serverless.SampleAdapter1.dll";

            var output = await RunOutOfProcess(adapterpath, input);

            return output;
        }

        async Task<string> RunOutOfProcess(string path, string input)
        {
            var tcs = new TaskCompletionSource<string>();
            var process = new Process
            {
                EnableRaisingEvents = true,
                StartInfo = new ProcessStartInfo("dotnet")
                {
                    Arguments = $"\"{path}\"",
                    WorkingDirectory = Path.GetDirectoryName(path),
                    UseShellExecute = false,

                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,

                    StandardInputEncoding = Encoding.UTF8,
                    StandardOutputEncoding = Encoding.UTF8,
                    //StandardErrorEncoding = Encoding.UTF8,
                }
            };

            var timer = new Timer(
                callback: state => process.Kill(),
                state: null,
                dueTime: TimeSpan.FromMinutes(15),
                period: Timeout.InfiniteTimeSpan);

            var output = "";

            process.OutputDataReceived += (sender, args) =>
            {
                if (args.Data != null) output += args.Data;
            };

            process.Exited += (sender, args) =>
            {
                try
                {
                    if (process.ExitCode != 0)
                        throw new SWException($"{process.ExitCode}, {process.StandardError.ReadToEnd()}");
                    else
                    {
                        //while (string.IsNullOrEmpty(output))
                        //{
                        //    Thread.Sleep(50);
                        //}
                        //resp = JsonConvert.DeserializeObject<PipelineResponse>(output);

                        //if (output is null)
                        //{
                        //    throw new ArgumentNullException($"{nameof(resp)} is null with the output: {output} And isDataReceived: {dataReceived.ToString()}");
                        //}

                        if (!tcs.TrySetResult(output))
                        {
                            throw new SWException($"couldnt transition the task to Rantocompletion state with output: {output}");
                        }
                    }
                    //tcs.SetResult(JsonConvert.DeserializeObject<PipelineResponse>(output));

                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
                finally
                {
                    process.Dispose();
                    timer.Dispose();
                }
            };

            if (!process.Start()) throw new SWException("Process reused!");
            await process.StandardInput.WriteAsync(input.Replace("\n", ""));
            await process.StandardInput.WriteLineAsync();
            //await process.StandardInput.FlushAsync();
            //process.StandardInput.Close();

            process.BeginOutputReadLine();

            return await tcs.Task;
        }

    }
}
