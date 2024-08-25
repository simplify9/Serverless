using System.Collections.Generic;

namespace SW.Serverless.Desktop
{
    public class CloudConnection
    {
        public string AccessKeyId { get; set; }

        public string SecretAccessKey { get; set; }

        public string BucketName { get; set; }

        public string ServiceUrl { get; set; }

    }
    public class Options
    {
        public Options()
        {
            CloudConnections = new List<CloudConnection>();
        }
        public List<CloudConnection> CloudConnections { get; set; }
    }
}
