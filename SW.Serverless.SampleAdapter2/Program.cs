using SW.Serverless.Sdk;
using System;
using System.Data.Common;
using System.Threading.Tasks;

namespace SW.Serverless.SampleAdapter2
{
    class Program
    {
        async static Task Main(string[] args)
        {
            AdapterLogger.LogInformation($"Started, arguments: {string.Join(",", Environment.GetCommandLineArgs())}");
            await Runner.Run(new Handler());
        }
    }
}
