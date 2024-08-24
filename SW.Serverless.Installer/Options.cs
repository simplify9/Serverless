using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;

namespace SW.Serverless.Installer
{
    public class CliOptions
    {
        //[Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
        //public bool Verbose { get; set; }
        [Option('p', "provider", HelpText = "Provider for storage (as/s3/oc).")]
        public string Provider { get; set; }

        [Option('a', "accesskey", HelpText = "Access key for storage.")]
        public string AccessKeyId { get; set; }

        [Option('s', "secret", HelpText = "Secret access key for storage.")]
        public string SecretAccessKey { get; set; }

        [Option('b', "bucketname", HelpText = "Bucket name for storage.")]
        public string BucketName { get; set; }


        [Option('u', "url", HelpText = "Service Url for storage.")]
        public string ServiceUrl { get; set; }


        [Option('c', "cloudfilesconfigpath", HelpText = "Json cloud files config path. (required for Oracle Cloud)")]
        public string CloudFilesConfigPath { get; set; }

        [Option('v', "version",
            HelpText =
                "Semantic version in the format major.minor.patch, or specify 'major', 'minor', or 'patch' to auto-increment the respective part.")]
        public string Version { get; set; }

        [Value(0, HelpText = "Path to project file (csproj)")]
        public string ProjectPath { get; set; }

        [Value(1, HelpText = "Adapter Id")] public string AdapterId { get; set; }
    }

    public class ServerlessUploadOptions
    {
        public string Provider { get; set; }

        public string AccessKeyId { get; set; }

        public string SecretAccessKey { get; set; }

        public string BucketName { get; set; }

        public string ServiceUrl { get; set; }


        public string RSAKey { get; set; }

        public string UserId { get; set; }

        public string FingerPrint { get; set; }

        public string TenantId { get; set; }

        public string Region { get; set; }


        public string Version { get; set; }


        public string AdapterId { get; set; }
    }
}