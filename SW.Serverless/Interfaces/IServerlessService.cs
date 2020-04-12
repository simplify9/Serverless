using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SW.Serverless
{
    interface IServerlessService
    {
        Task<string> Run(string adapterId, string input);
    }
}
