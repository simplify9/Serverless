using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;

namespace SW.Serverless.Installer
{
    public class Options
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

        //oracle cloud

        [Option('k', "rsakey", HelpText = "Oracle cloud rsa key.")]
        public string RSAKey { get; set; }

        [Option('r', "userid", HelpText = "Oracle cloud user id.")]
        public string UserId { get; set; }

        [Option('f', "fingerprint", HelpText = "Oracle cloud finger print.")]
        public string FingerPrint { get; set; }

        [Option('t', "tenantjd", HelpText = "Oracle cloud tenant id.")]
        public string TenantId { get; set; }

        [Option('r', "region", HelpText = "Oracle cloud region.")]
        public string Region { get; set; }

        [Option('c', "configpath", HelpText = "Oracle cloud config path.")]
        public string ConfigPath { get; set; }


        [Option('v', "version",
            HelpText =
                "Semantic version in the format major.minor.patch, or specify 'major', 'minor', or 'patch' to auto-increment the respective part.")]
        public string Version { get; set; }

        [Value(0, HelpText = "Path to project file (csproj)")]
        public string ProjectPath { get; set; }

        [Value(1, HelpText = "Adapter Id")] public string AdapterId { get; set; }
    }
}