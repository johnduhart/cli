using Microsoft.Extensions.DependencyModel.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
