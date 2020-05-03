using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SW.CloudFiles.Extensions;
using SW.PrimitiveTypes;

namespace SW.Serverless.SampleWeb
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddServerless();
            services.AddCloudFiles();
            //services.AddServerless(config =>
            //{
            //    config.CloudFilesOptions = new CloudFilesOptions
            //    {
            //        AccessKeyId = "R3LNFRKWMAC4OCCRICS5",
            //        SecretAccessKey = "YPyyTdxs+lZMQEtYIDRK9lkIzjJrCKXinE3OfKEfc7k",
            //        ServiceUrl = "https://fra1.digitaloceanspaces.com",
            //        BucketName = "sf9"
            //    };
            //    config.AdapterMetadataCacheDuration = 1;
            //    //config.IdleTimeout

            //});
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
