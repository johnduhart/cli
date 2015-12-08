using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.ProjectModel;
using Microsoft.DotNet.ProjectModel.Compilation;

namespace Microsoft.Extensions.DependencyModel
{
    public static class DependencyContextBuilder
    {
        public static  DependencyContext FromLibraryExporter(LibraryExporter libraryExporter, string target, string runtime)
        {
            var dependencies = libraryExporter.GetDependencies();

            return new DependencyContext(target, runtime,
                GetLibraries(dependencies, export => export.CompilationAssemblies),
                GetLibraries(dependencies, export => export.RuntimeAssemblies));
        }

        private static Library[] GetLibraries(IEnumerable<LibraryExport> dependencies, Func<LibraryExport, IEnumerable<LibraryAsset>> assemblySelector)
        {
            return dependencies.Select(export => new Library(
                export.Library.Identity.Type.ToString().ToLowerInvariant(),
                export.Library.Identity.Name,
                export.Library.Identity.Version.ToString(),
                export.Library.Hash,
                assemblySelector(export).Select(a => a.RelativePath).ToArray(),
                export.Library.Dependencies.Select(d => new Dependency(d.Name, "???")).ToArray(),
                false // ???
                )).ToArray();
        }
    }
}
