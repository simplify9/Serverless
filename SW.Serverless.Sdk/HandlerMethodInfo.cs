﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace SW.Serverless.Sdk
{
    class HandlerMethodInfo
    {
        public MethodInfo MethodInfo { get; set; }
        public bool Void { get; set; }
        //public bool Parameterless { get; set; }
        public Type ParameterType { get; set; }
        
    }
}
