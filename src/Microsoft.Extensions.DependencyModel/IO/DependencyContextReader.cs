using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Extensions.DependencyModel.IO
{
    public class DependencyContextReader
    {
        private const char VersionSeperator = '/';

        private const string CompileTimeAssembliesKey = "compile";

        private const string RunTimeAssembliesKey = "runtime";

        private const string LibrariesPropertyName = "libraries";

        private const string TargetsPropertyName = "targets";

        private const string DependenciesPropertyName = "dependencies";

        private const string Sha512PropertyName = "sha512";

        private const string TypePropertyName = "type";

        private const string ServiceablePropertyName = "serviceable";

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
            var libraryStubs = ReadLibraryStubs((JObject) root[LibrariesPropertyName]);
            var targetsObject = (IEnumerable<KeyValuePair<string, JToken>>) root[TargetsPropertyName];

            var targets = new List<Target>();
            foreach (var target in targetsObject.Where(t => !t.Key.Contains(VersionSeperator)))
            {
                var libraries = ReadLibraries((JObject) target.Value, true, libraryStubs);
                var runtimes = new List<Runtime>();

                foreach (var runtime in targetsObject.Where(t => t.Key.StartsWith(target.Key) && t.Key != target.Key))
                {
                    var rid = runtime.Key.Substring(target.Key.Length + 1);
                    var runtimeLibraries = ReadLibraries((JObject) target.Value, false, libraryStubs);
                    runtimes.Add(new Runtime(rid, runtimeLibraries));
                }
                targets.Add(new Target(target.Key, libraries, runtimes.ToArray()));
            }
            return new DependencyContext(targets.ToArray());
        }

        private Library[] ReadLibraries(JObject librariesObject, bool compileTime,
            Dictionary<string, LibraryStub> libraryStubs)
        {
            return librariesObject.Properties().Select(property => ReadLibrary(property, true, libraryStubs)).ToArray();
        }

        private Library ReadLibrary(JProperty property, bool compileTime, Dictionary<string, LibraryStub> libraryStubs)
        {
            var nameWithVersion = property.Name;
            LibraryStub stub;

            if (!libraryStubs.TryGetValue(nameWithVersion, out stub))
            {
                throw new InvalidOperationException($"Cannot find library information for {nameWithVersion}");
            }

            var seperatorPosition = nameWithVersion.IndexOf(VersionSeperator);

            var name = nameWithVersion.Substring(0, seperatorPosition);
            var version = nameWithVersion.Substring(seperatorPosition);

            var libraryObject = (JObject) property.Value;

            var dependencies = ((JObject) libraryObject[DependenciesPropertyName])
                ?.Properties().Select(p => new Dependency(p.Name, (string) p.Value)).ToArray() ??
                               Array.Empty<Dependency>();

            var assemblies = ((JObject) libraryObject[compileTime ? CompileTimeAssembliesKey : RunTimeAssembliesKey])
                ?.Properties().Select(p => p.Name).ToArray() ?? Array.Empty<string>();

            return new Library(stub.Type, name, version, stub.Hash, assemblies, dependencies, stub.Serviceable);
        }

        private Dictionary<string, LibraryStub> ReadLibraryStubs(JObject librariesObject)
        {
            var libraries = new Dictionary<string, LibraryStub>();
            foreach (var libraryProperty in librariesObject)
            {
                var value = (JObject) libraryProperty.Value;
                var stub = new LibraryStub
                {
                    Name = libraryProperty.Key,
                    Hash = value[Sha512PropertyName]?.Value<string>(),
                    Type = value[TypePropertyName].Value<string>(),
                    Serviceable = value[ServiceablePropertyName]?.Value<bool>() == true
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

            public bool Serviceable;
        }
    }
}