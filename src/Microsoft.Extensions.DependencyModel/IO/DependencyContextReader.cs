// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

        private bool IsRuntimeTarget(string name) => name.Contains(VersionSeperator);

        private DependencyContext Read(JObject root)
        {
            var libraryStubs = ReadLibraryStubs((JObject) root[LibrariesPropertyName]);
            var targetsObject = (IEnumerable<KeyValuePair<string, JToken>>) root[TargetsPropertyName];

            var runtimeTargetObject = (JObject)targetsObject.First(t => IsRuntimeTarget(t.Key)).Value;
            var compileTargetObject = (JObject)targetsObject.First(t => !IsRuntimeTarget(t.Key)).Value;

            return new DependencyContext(
                ReadLibraries(runtimeTargetObject, true, libraryStubs),
                ReadLibraries(compileTargetObject, false, libraryStubs)
                );
        }

        private Library[] ReadLibraries(JObject librariesObject, bool runtime, Dictionary<string, LibraryStub> libraryStubs)
        {
            return librariesObject.Properties().Select(property => ReadLibrary(property, runtime, libraryStubs)).ToArray();
        }

        private Library ReadLibrary(JProperty property, bool runtime, Dictionary<string, LibraryStub> libraryStubs)
        {
            var nameWithVersion = property.Name;
            LibraryStub stub;

            if (!libraryStubs.TryGetValue(nameWithVersion, out stub))
            {
                throw new InvalidOperationException($"Cannot find library information for {nameWithVersion}");
            }

            var seperatorPosition = nameWithVersion.IndexOf(VersionSeperator);

            var name = nameWithVersion.Substring(0, seperatorPosition);
            var version = nameWithVersion.Substring(seperatorPosition + 1);

            var libraryObject = (JObject) property.Value;

            var dependencies = ReadDependencies(libraryObject);
            var assemblies = ReadAssemblies(libraryObject, runtime);

            return new Library(stub.Type, name, version, stub.Hash, assemblies, dependencies, stub.Serviceable);
        }

        private static string[] ReadAssemblies(JObject libraryObject, bool runtime)
        {
            var assembliesObject = (JObject) libraryObject[runtime ? RunTimeAssembliesKey : CompileTimeAssembliesKey];

            if (assembliesObject == null)
            {
                return Array.Empty<string>();
            }

            return assembliesObject.Properties().Select(p => p.Name).ToArray();
        }

        private static Dependency[] ReadDependencies(JObject libraryObject)
        {
            var dependenciesObject = ((JObject) libraryObject[DependenciesPropertyName]);

            if (dependenciesObject == null)
            {
                return Array.Empty<Dependency>();
            }

            return dependenciesObject.Properties().Select(p => new Dependency(p.Name, (string) p.Value)).ToArray();
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