using SW.Serverless.Sdk;
using System;
using System.Threading.Tasks;

namespace SW.Serverless.SampleAdapter1
{
    class Program  
    {
        async static Task Main(string[] args)
        {
            //Console.ReadLine();
            await Runner.Run(new Handler());

        }


    }
}
