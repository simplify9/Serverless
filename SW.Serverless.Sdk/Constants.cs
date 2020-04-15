using System;
using System.Collections.Generic;
using System.Text;

namespace SW.Serverless.Sdk
{
    public static class Constants
    {
        public const string Delimiter = "#!#";
        public const string NullIdentifier = "{{null}}";
        public const string NewLineIdentifier = "{{newline}}";
        public const string ErrorIdentifier = "{{error}}";
        public const string QuitCommand = "{{quit}}";
        public const string LogInformationIdentifier = "{{log.information}}";
        public const string LogWarningIdentifier = "{{log.warning}}";
        public const string LogErrorIdentifier = "{{log.error}}";
    }
}
