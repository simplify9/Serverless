using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using SW.PrimitiveTypes;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

using SW.Searchy;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System.Threading;

namespace SW.Serverless
{
    public class AdapterService
    {
        private static readonly SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);
        private readonly ILogger<AdapterService> logger;
        private readonly ServerlessOptions serverlessOptions;
        private readonly ICloudFilesService cloudFilesService;
        private readonly IMemoryCache memoryCache;

        public AdapterService(ILogger<AdapterService> logger, ServerlessOptions serverlessOptions, ICloudFilesService cloudFilesService, IMemoryCache memoryCache)
        {
            this.logger = logger;
            this.serverlessOptions = serverlessOptions;
            this.cloudFilesService = cloudFilesService;
            this.memoryCache = memoryCache;
        }

        public async Task<string> Install(string adapterId)
        {
            var adapterConfig = await GetAdapterConfig(adapterId);
            var adapterDiretoryPath = $"{serverlessOptions.AdapterRootPath}/{adapterConfig.Signature}";//Path.Combine(ConfigurationManager.AppSettings["PluginsFolderPath"], BitConverter.ToString(_data).Replace("-", "").ToLower());
            var adapterPath = Path.GetFullPath($"{adapterDiretoryPath}/{adapterConfig.EntryAssembly}");

            await semaphoreSlim.WaitAsync();
            try
            {
                if (!Directory.Exists(adapterDiretoryPath))
                {
                    Directory.CreateDirectory(adapterDiretoryPath);
                    try
                    {
                        logger.LogInformation($"Adapter not installed, installing adapter: '{adapterPath}'");

                        using var stream = await cloudFilesService.OpenReadAcync($"adapters/{adapterId}/content.zip");
                        //using (var zipdata = new MemoryStream(adpContent))
                        using var archive = new ZipArchive(stream);

                        foreach (var entry in archive.Entries)
                            entry.ExtractToFile($"{adapterDiretoryPath}/{entry.Name}");

                        //Process.Start("chmod", $"755 {adapterPath}").WaitForExit(5000);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, $"Failed to install adapter: '{adapterPath}'");
                        Directory.Delete(adapterDiretoryPath, true);
                        throw ex;
                    }
                }
            }
            finally
            {
                semaphoreSlim.Release();
            }

            return adapterPath;
        }

        async Task<AdapterConfig> GetAdapterConfig(string adapterId)
        {
            if (memoryCache.TryGetValue($"adapters:{adapterId}", out AdapterConfig adapterConfig))
                return adapterConfig;


            using var stream = await cloudFilesService.OpenReadAcync($"adapters/{adapterId}/config.json");
            using var streamReader = new StreamReader(stream);

            var data = await streamReader.ReadToEndAsync();
            adapterConfig = JsonConvert.DeserializeObject<AdapterConfig>(data);
            return memoryCache.Set($"adapters:{adapterId}", adapterConfig, TimeSpan.FromMinutes(5));

        }

    }
}
