using System;
using System.Collections.Generic;
using System.Text;

namespace SW.Serverless
{
    public class ServerlessOptions
    {
        public string AccessKeyId { get; set; }
        public string SecretAccessKey { get; set; }
        public string BucketName { get; set; }
        public string ServiceUrl { get; set; }
        public string AdapterLocalPath { get; set; } = "./adapters";
        public int AdapterMetadataCacheDuration { get; set; } = 5;
        public string AdapterRemotePath { get; set; } = "adapters";
        public int CommandTimeout { get; set; } = 30;

    }
}
