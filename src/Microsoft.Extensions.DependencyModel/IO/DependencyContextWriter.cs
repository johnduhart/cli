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
}