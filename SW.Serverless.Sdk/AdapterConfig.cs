using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace SW.Serverless.Sdk
{
    public  class AdapterConfig 
    {
        public AdapterConfig(string[] args)
        {
        }

        //private static 
        public  string this[string key] => throw new NotImplementedException();

       // public static IEnumerable<string> Keys => throw new NotImplementedException();

        //public IEnumerable<string> Values => throw new NotImplementedException();

        //public int Count => throw new NotImplementedException();

        //public bool ContainsKey(string key)
        //{
        //    throw new NotImplementedException();
        //}

        //public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        //{
        //    throw new NotImplementedException();
        //}

        public bool TryGetValue(string key, out string value)
        {
            throw new NotImplementedException();
        }

        //IEnumerator IEnumerable.GetEnumerator()
        //{
        //    throw new NotImplementedException();
        //}
    }
}
