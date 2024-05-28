using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;

namespace SW.Serverless.Installer
{
    class Options
    {
        //[Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
        //public bool Verbose { get; set; }
        [Option('p', "provider", HelpText = "Provider for storage (azure/s3).")]
        public string Provider { get; set; }

        [Option('a', "accesskey", Required = true, HelpText = "Access key for storage.")]
        public string AccessKeyId { get; set; }

        [Option('s', "secret", Required = true, HelpText = "Secret access key for storage.")]
        public string SecretAccessKey { get; set; }

        [Option('b', "bucketname", Required = true, HelpText = "Bucket name for storage.")]
        public string BucketName { get; set; }

        [Option('u', "url", Required = true, HelpText = "Service Url for storage.")]
        public string ServiceUrl { get; set; }

        [Option('v', "version",
            HelpText =
                "Semantic version in the format major.minor.patch, or specify 'major', 'minor', or 'patch' to auto-increment the respective part.")]
        public string Version { get; set; }

        [Value(0, Required = true, HelpText = "Path to project file (csproj)")]
        public string ProjectPath { get; set; }

        [Value(1, Required = true, HelpText = "Adapter Id")]
        public string AdapterId { get; set; }
    }
}