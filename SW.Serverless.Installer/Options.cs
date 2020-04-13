using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;

namespace SW.Serverless.Installer
{
    class Options
    {
        [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
        public bool Verbose { get; set; }

        [Option('a', "accesskey", Required = true, HelpText = "Access key for storage.")]
        public string AccessKeyId { get; set; }

        [Option('s', "secret", Required = true, HelpText = "Secret access key for storage.")]
        public string SecretAccessKey { get; set; }

        [Option('b', "bucketname", Required = true, HelpText = "Bucket name for storage.")]
        public string BucketName { get; set; }

        [Option('u', "url", Required = true, HelpText = "Service Url for storage.")]
        public string ServiceUrl { get; set; }

        [Value(0, Required = true, HelpText = "Path to project file (csproj)")]
        public string ProjectPath { get; set; }

        [Value(1, Required = true)]
        public string AdapterName { get; set; }
    }
}
