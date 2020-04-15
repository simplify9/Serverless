using SW.Serverless.Sdk;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SW.Serverless.SampleAdapter2
{
    class Handler
    {
        async public Task TestVoid(string input)
        {

        }

        async public Task<string> TestString(string input)
        {
            AdapterLogger.LogWarning("not implemented.");
            AdapterLogger.LogWarning("not implemented.");
            throw new NotImplementedException();
        }

        //async public Task<int> TestInt(string input)
        //{
        //    return 2;
        //}


    }
}
