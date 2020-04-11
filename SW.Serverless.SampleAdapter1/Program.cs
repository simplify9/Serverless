using SW.Serverless.Sdk;
using System;
using System.Threading.Tasks;

namespace SW.Serverless.SampleAdapter1
{
    class Program  : AdapterBase
    {
        async static Task Main(string[] args)
        {
            var program = new Program();
            await program.Run();
        }

        async protected override Task<string> Handle(string input)
        {
            return input;
        }
    }
}
