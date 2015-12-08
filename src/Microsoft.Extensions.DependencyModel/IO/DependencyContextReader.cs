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
    public class DependencyContextWriter
    {
        public void Write(DependencyContext context, Stream stream)
        {
            using (var writer = new StreamWriter(stream))
            {
                using (var jsonWriter = new JsonTextWriter(writer))
                {
                    Write(context).WriteTo(jsonWriter);
                }
            }
        }

        private JObject Write(DependencyContext context)
        {
            return new JObject(
                new JProperty(DependencyContextStrings.TargetsPropertyName, WriteTargets(context)),
                new JProperty(DependencyContextStrings.LibrariesPropertyName, WriteLibraries(context))
                );

        }

        private JObject WriteTargets(DependencyContext context)
        {
            return new JObject(
                        new JProperty(context.Target, WriteTarget(context.CompileLibraries, false)),
                        new JProperty(context.Target + DependencyContextStrings.VersionSeperator + context.Runtime,
                            WriteTarget(context.RuntimeLibraries, true))
                        );
        }

        private JObject WriteTarget(IReadOnlyList<Library> libraries, bool runtime)
        {
            return new JObject(
                libraries.Select(l => new JProperty(l.PackageName + DependencyContextStrings.VersionSeperator + l.Version, WriteTargetLibrary(l, runtime))));
        }

        private JObject WriteTargetLibrary(Library library, bool runtime)
        {
            return new JObject(
                new JProperty(DependencyContextStrings.DependenciesPropertyName, WriteDependencies(library.Dependencies)),
                new JProperty(runtime? DependencyContextStrings.RunTimeAssembliesKey : DependencyContextStrings.CompileTimeAssembliesKey,
                    WriteAssemblies(library.Assemblies))
                );
        }

        private JObject WriteAssemblies(IReadOnlyList<string> assemblies)
        {
            return new JObject(assemblies.Select(a => new JProperty(a, new JObject())));
        }

        private JObject WriteDependencies(IReadOnlyList<Dependency> dependencies)
        {
            return new JObject(
                dependencies.Select(d => new JProperty(d.Name, d.Version))
                );
        }

        private JObject WriteLibraries(DependencyContext context)
        {
            var allLibraries =
                context.RuntimeLibraries.Concat(context.CompileLibraries)
                    .GroupBy(l => l.PackageName + DependencyContextStrings.VersionSeperator + l.Version);

            return new JObject(allLibraries.Select(l=> new JProperty(l.Key, WriteLibrary(l.First()))));
        }

        private JObject WriteLibrary(Library library)
        {
            return new JObject(
                new JProperty(DependencyContextStrings.TypePropertyName, library.LibraryType),
                new JProperty(DependencyContextStrings.ServiceablePropertyName, library.Serviceable),
                new JProperty(DependencyContextStrings.Sha512PropertyName, library.Hash)
                );
        }
    }

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

        private bool IsRuntimeTarget(string name) => name.Contains(DependencyContextStrings.VersionSeperator);

        private DependencyContext Read(JObject root)
        {
            var libraryStubs = ReadLibraryStubs((JObject) root[DependencyContextStrings.LibrariesPropertyName]);
            var targetsObject = (IEnumerable<KeyValuePair<string, JToken>>) root[DependencyContextStrings.TargetsPropertyName];

            var runtimeTargetProperty = targetsObject.First(t => IsRuntimeTarget(t.Key));
            var compileTargetProperty = targetsObject.First(t => !IsRuntimeTarget(t.Key));

            return new DependencyContext(
                compileTargetProperty.Key,
                runtimeTargetProperty.Key.Substring(compileTargetProperty.Key.Length + 1),
                ReadLibraries((JObject)runtimeTargetProperty.Value, true, libraryStubs),
                ReadLibraries((JObject)compileTargetProperty.Value, false, libraryStubs)
                );
        }

        private Library[] ReadLibraries(JObject librariesObject, bool runtime, Dictionary<string, DependencyContextReader.LibraryStub> libraryStubs)
        {
            return librariesObject.Properties().Select(property => ReadLibrary(property, runtime, libraryStubs)).ToArray();
        }

        private Library ReadLibrary(JProperty property, bool runtime, Dictionary<string, DependencyContextReader.LibraryStub> libraryStubs)
        {
            var nameWithVersion = property.Name;
            DependencyContextReader.LibraryStub stub;

            if (!libraryStubs.TryGetValue(nameWithVersion, out stub))
            {
                throw new InvalidOperationException($"Cannot find library information for {nameWithVersion}");
            }

            var seperatorPosition = nameWithVersion.IndexOf(DependencyContextStrings.VersionSeperator);

            var name = nameWithVersion.Substring(0, seperatorPosition);
            var version = nameWithVersion.Substring(seperatorPosition + 1);

            var libraryObject = (JObject) property.Value;

            var dependencies = ReadDependencies(libraryObject);
            var assemblies = ReadAssemblies(libraryObject, runtime);

            return new Library(stub.Type, name, version, stub.Hash, assemblies, dependencies, stub.Serviceable);
        }

        private static string[] ReadAssemblies(JObject libraryObject, bool runtime)
        {
            var assembliesObject = (JObject) libraryObject[runtime ? DependencyContextStrings.RunTimeAssembliesKey : DependencyContextStrings.CompileTimeAssembliesKey];

            if (assembliesObject == null)
            {
                return Array.Empty<string>();
            }

            return assembliesObject.Properties().Select(p => p.Name).ToArray();
        }

        private static Dependency[] ReadDependencies(JObject libraryObject)
        {
            var dependenciesObject = ((JObject) libraryObject[DependencyContextStrings.DependenciesPropertyName]);

            if (dependenciesObject == null)
            {
                return Array.Empty<Dependency>();
            }

            return dependenciesObject.Properties().Select(p => new Dependency(p.Name, (string) p.Value)).ToArray();
        }

        private Dictionary<string, DependencyContextReader.LibraryStub> ReadLibraryStubs(JObject librariesObject)
        {
            var libraries = new Dictionary<string, DependencyContextReader.LibraryStub>();
            foreach (var libraryProperty in librariesObject)
            {
                var value = (JObject) libraryProperty.Value;
                var stub = new DependencyContextReader.LibraryStub
                {
                    Name = libraryProperty.Key,
                    Hash = value[DependencyContextStrings.Sha512PropertyName]?.Value<string>(),
                    Type = value[DependencyContextStrings.TypePropertyName].Value<string>(),
                    Serviceable = value[DependencyContextStrings.ServiceablePropertyName]?.Value<bool>() == true
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