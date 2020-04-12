
using Microsoft.Extensions.DependencyInjection;
using SW.PrimitiveTypes;
using System;
using System.Reflection;

namespace SW.Serverless
{
    public static class IServiceCollectionExtensions
    {



        public static IServiceCollection AddServerless(this IServiceCollection services, Action<ServerlessOptions> configure)
        {

            var serverlessOptions = new ServerlessOptions();
            if (configure != null) configure.Invoke(serverlessOptions);
            services.AddSingleton(serverlessOptions);
            //services.AddSingleton<AdapterService>();
            services.AddSingleton<ServerlessServiceOld>();
            services.AddTransient<ServerlessService>();


            return services;
        }
    }
}
