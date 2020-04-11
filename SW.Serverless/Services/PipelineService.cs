using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using SW.PrimitiveTypes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SW.Serverless
{
    public class PipelineService
    {

        readonly AdapterService adapterService;

        public PipelineService(AdapterService adapterService)
        {
            this.adapterService = adapterService;
        }



        public async Task<string> Run(string adapterId, string input)
        {
            var adapterpath = await adapterService.Install(adapterId);

            var output = await RunOutOfProcess(adapterpath, input);

            return output;
        }

        Task<string> RunOutOfProcess(string path, string input)
        {
            //var input = JsonConvert.SerializeObject(pipelineRequest);
            var tcs = new TaskCompletionSource<string>();
            var process = new Process
            {
                EnableRaisingEvents = true,
                StartInfo = new ProcessStartInfo("dotnet")
                {
                    Arguments = $"\"{path}\"",
                    WorkingDirectory = Path.GetDirectoryName(path),
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                }
            };

            var timer = new Timer(
                callback: state => process.Kill(),
                state: null,
                dueTime: TimeSpan.FromMinutes(15),
                period: Timeout.InfiniteTimeSpan);

            var output = "";
            //bool dataReceived = false;
            //PipelineResponse resp;
            process.OutputDataReceived += (sender, args) =>
            {
               if (args.Data != null) output += args.Data;

               //dataReceived = true;
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

            process.StandardInput.WriteLine(input);
            process.StandardInput.Flush();
            process.BeginOutputReadLine();

            return tcs.Task;
        }
    }
}
