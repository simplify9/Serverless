using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;

namespace SW.Serverless.Installer
{
    public class Options
    {
        [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
        public bool Verbose { get; set; }

        [Value(0, Required = true)]
        public string Project { get; set; }
    }
}
