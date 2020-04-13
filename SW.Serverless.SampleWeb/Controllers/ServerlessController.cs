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
            //this.pipelineService = pipelineService;
            this.serviceProvider = serviceProvider;
        }

        [HttpPost("{adapterId}")]
        public async Task<IActionResult> Invoke(string adapterId)
        {
            using var stream = new StreamReader(Request.Body);

            var serverless = serviceProvider.GetService<ServerlessService>();

            await serverless.StartAsync(adapterId);
            var input = await stream.ReadToEndAsync();
            string result = null;
            //for (var index = 0; index< 10000; index++)
            //{
            //     result = await serverless.InvokeAsync("TestString", input);
            //}
            result = await serverless.InvokeAsync("TestString", input);
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
