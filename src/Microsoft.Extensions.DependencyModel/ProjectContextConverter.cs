using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.ProjectModel;
using Microsoft.DotNet.ProjectModel.Compilation;

namespace Microsoft.Extensions.DependencyModel
{
    public static class ProjectContextConverter
    {
        public static  DependencyContext ToDependencyContext(LibraryExporter libraryExporter, string target, string runtime)
        {
            var dependencies = libraryExporter.GetDependencies();

            return new DependencyContext(target, runtime,
                GetLibraries(dependencies, e => e.CompilationAssemblies),
                GetLibraries(dependencies, e => e.RuntimeAssemblies));
        }

        private static Library[] GetLibraries(IEnumerable<LibraryExport> dependencies, Func<LibraryExport, IEnumerable<LibraryAsset>> assemblySelector)
        {
            return dependencies.Select(l => new Library(
                l.Library.Identity.Type.ToString().ToLowerInvariant(),
                l.Library.Identity.Name,
                l.Library.Identity.Version.ToString(),
                l.Library.Hash,
                assemblySelector(l).Select(a => a.RelativePath).ToArray(),
                l.Library.Dependencies.Select(d => new Dependency(d.Name, "???")).ToArray(),
                false // ???
                )).ToArray();
        }
    }
}
