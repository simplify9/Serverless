using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SW.PrimitiveTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SW.Serverless.Controllers
{

    [Route("serverless")]
    [ApiController]
    //[Authorize(AuthenticationSchemes = "Bearer")]
    [AllowAnonymous]
    public class ServerlessController : ControllerBase
    {
        private readonly PipelineService pipelineService;
        private readonly ICloudFilesService cloudFilesService;

        public ServerlessController(PipelineService pipelineService, ICloudFilesService cloudFilesService)
        {
            this.pipelineService = pipelineService;
            this.cloudFilesService = cloudFilesService;
        }

        [HttpPost("{adapterId}")]
        public async Task<IActionResult> Invoke(string adapterId)
        {
            using (var stream = new StreamReader(Request.Body))
            {
                var input = await stream.ReadToEndAsync();
                var result = await pipelineService.Run(adapterId, input);
                return Ok(result);
            }
        }


        [HttpPut("admin/adapters/{adapterId}")]
        public async Task<IActionResult> Install(string adapterId, [FromBody]InstallAdapter installAdapter)
        {

            var adapterConfig = new AdapterConfig
            {
                EntryAssembly = installAdapter.EntryAssembly,
                Signature = "123"
            };

            await cloudFilesService.WriteTextAcync("adapters/{adapterId}", new WriteFileSettings
            {
                ContentType = "application/json",
                Key = ""
            });

            return Ok();

        }
    }
}
