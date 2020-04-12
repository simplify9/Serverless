using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SW.Serverless.SampleAdapter1
{
    class Handler
    {
        async public Task TestVoid(string input)
        {

        }

        async public Task<string> TestString(string input)
        {
            return input;
        }

        //async public Task<int> TestInt(string input)
        //{
        //    return 2;
        //}


    }
}
