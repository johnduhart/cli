using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Extensions.DependencyModel.IO
{
    public class DependencyContextReader
    {
        public DependencyContext Read(Stream stream)
        {
            using (var streamReader = new StreamReader(stream))
            {
                using (var reader = new JsonTextReader(streamReader))
                {
                    var root = JObject.Load(reader);
                    return Read(root);
                }
            }
        }

        private DependencyContext Read(JObject root)
        {
            var libraries = ReadLibraries((JObject)root["libraries"]);
            return ReadTargets(libraries, root);
        }

        private Dictionary<string, LibraryStub> ReadLibraries(JObject librariesObject)
        {
            var libraries = new Dictionary<string, LibraryStub>();
            foreach (KeyValuePair<string, JToken> libraryProperty in librariesObject)
            {
                var value = (JObject)libraryProperty.Value;
                var stub = new LibraryStub()
                {
                    Name = libraryProperty.Key,
                    Hash = value.Property("hash").Value<string>(),
                    Type = value.Property("type").Value<string>(),
                    Serviceble = value.Property("serviceble").Value<bool>(),
                };
                libraries.Add(stub.Name, stub);
            }
            return libraries;
        }

        private struct LibraryStub
        {
            public string Name;

            public string Hash;

            public string Type;

            public bool Serviceble;
        }
    }
}
