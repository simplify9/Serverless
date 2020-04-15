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
            return "hello";
        }

        async public Task<string> Test3()
        {
            AdapterLogger.LogWarning("not implemented.");
            AdapterLogger.LogWarning("not implemented.");
            throw new NotImplementedException();
        }


        async public Task TestString2()
        {
            AdapterLogger.LogWarning("not implemented.");
            AdapterLogger.LogWarning("not implemented.");
            throw new NotImplementedException();
        }


    }
}
