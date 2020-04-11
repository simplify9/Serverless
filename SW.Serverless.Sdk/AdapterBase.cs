using System;
using System.Threading.Tasks;

namespace SW.Serverless.Sdk
{
    public abstract class AdapterBase
    {
        protected async Task Run()
        {
            var input = Console.ReadLine();
            var result = await Handle(input);

            Console.WriteLine(result);
            Console.Out.Flush();

        }
        protected abstract Task<string> Handle(string input);

    }
}
