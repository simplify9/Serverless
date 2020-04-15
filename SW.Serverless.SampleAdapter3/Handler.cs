using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SW.Serverless.SampleAdapter3
{
    class Handler
    {

        
        async public Task TestVoid(string input)
        {

        }

        async public Task<string> ListFiles(string input)
        {
             throw new Exception("bad login");
        }


        async public Task<string> GetFile(string input)
        {
            return "hello from sampleadapter3";
        }        //async public Task<int> TestInt(string input)
        //{
        //    return 2;
        //}


    }
}
