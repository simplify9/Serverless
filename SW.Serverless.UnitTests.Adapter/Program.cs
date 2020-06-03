using SW.Serverless.Sdk;
using System;
using System.Data.Common;
using System.Threading.Tasks;

namespace SW.Serverless.SampleAdapter2
{
    class Program
    {



        //build service collection
        async static Task Main(string[] args) => await Runner.Run(new Handler());

    }
}
