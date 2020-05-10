using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace SW.Serverless.Sdk
{
    public static class IReadOnlyDictionaryExtensions
    {
        public static string GetOrDefault(this IReadOnlyDictionary<string, string> dictionary, string key, string defaultValue)
        {
            dictionary.TryGetValue(key, out string val);
            if (val == null) val = defaultValue;
            return val;
        }

    }
}
