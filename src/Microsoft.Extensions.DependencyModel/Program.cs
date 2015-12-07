// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyModel.IO;
using System;
using System.IO;
using Newtonsoft.Json;

namespace Microsoft.Extensions.DependencyModel
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var c =  new DependencyContextReader().Read(File.OpenRead("d:\\dotnet-compile.deps.json"));
            Console.WriteLine(JsonConvert.SerializeObject(c,Formatting.Indented));
        }
    }
}
