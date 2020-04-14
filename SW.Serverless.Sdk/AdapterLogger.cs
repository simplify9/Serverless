using System;
using System.Collections.Generic;
using System.Text;

namespace SW.Serverless.Sdk
{
    public static class AdapterLogger 
    {
        public static void LogError(string message)
        {
            LogError(null, message);
        }
        public static void LogError(Exception exception, string message)
        {
            Console.Error.WriteLine($"{Constants.LogErrorIdentifier}{message?.Replace("\n", Constants.NewLineIdentifier)}{Constants.Delimiter}{exception?.ToString()}");
        }

        public static void LogWarning(string message)
        {
            LogWarning(null, message);
        }
        public static void LogWarning(Exception exception, string message)
        {
            Console.Error.WriteLine($"{Constants.LogWarningIdentifier}{message?.Replace("\n", Constants.NewLineIdentifier)}{Constants.Delimiter}{exception?.ToString()}");
        }

        public static void LogInformation(string message)
        {
            LogInformation(null, message);
        }
        public static void LogInformation(Exception exception, string message)
        {
            Console.Error.WriteLine($"{Constants.LogInformationIdentifier}{message?.Replace("\n", Constants.NewLineIdentifier)}{Constants.Delimiter}{exception?.ToString()}");
        }


    }
}
