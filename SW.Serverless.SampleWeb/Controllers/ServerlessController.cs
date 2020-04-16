using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using SW.PrimitiveTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SW.Serverless.SampleWeb.Controllers
{

    [Route("serverless")]
    [ApiController]
    //[Authorize(AuthenticationSchemes = "Bearer")]
    [AllowAnonymous]
    public class ServerlessController : ControllerBase
    {
        private readonly IServiceProvider serviceProvider;

        public ServerlessController(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        [HttpPost("{adapterId}/{method}")]
        public async Task<IActionResult> Invoke(string adapterId, string method)
        {
            using var stream = new StreamReader(Request.Body);

            var serverless = serviceProvider.GetService<IServerlessService>();

            await serverless.StartAsync(adapterId, @"C:\Users\Samer Awajan\source\repos\Serverless\SW.Serverless.SampleAdapter2\bin\Debug\netcoreapp3.1\SW.Serverless.SampleAdapter2.dll");
            var input = await stream.ReadToEndAsync();

            var result = await serverless.InvokeAsync(method, null);

            //await Task.Delay(TimeSpan.FromSeconds(30));
            //result = await serverless.InvokeAsync("TestString", input);

            return Ok(result);
            //return Ok();
        }


        //[HttpPut("admin/adapters/{adapterId}")]
        //public async Task<IActionResult> Install(string adapterId, [FromBody]InstallAdapter installAdapter)
        //{

        //    var adapterConfig = new AdapterMetadata
        //    {
        //        EntryAssembly = installAdapter.EntryAssembly,
        //        Hash = "123"
        //    };

        //    await cloudFilesService.WriteTextAcync("adapters/{adapterId}", new WriteFileSettings
        //    {
        //        ContentType = "application/json",
        //        Key = ""
        //    });

        //    return Ok();

        //}
    }
}
