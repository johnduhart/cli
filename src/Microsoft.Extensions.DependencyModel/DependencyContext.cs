// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Reflection;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyModel
{
    public class DependencyContext
    {
        private const string DepsExtension = ".deps";
        private const string DepsResourceName = "DependencyContext.json";

        public DependencyContext(string target, string runtime, Library[] compileLibraries, Library[] runtimeLibraries)
        {
            Target = target;
            Runtime = runtime;
            CompileLibraries = compileLibraries;
            RuntimeLibraries = runtimeLibraries;
        }

        public string Target { get; set; }

        public string Runtime { get; set; }

        public IReadOnlyList<Library> CompileLibraries { get; }

        public IReadOnlyList<Library> RuntimeLibraries { get; }


        public static DependencyContext Load()
        {
            var entryAssembly = (Assembly)typeof(Assembly).GetTypeInfo().GetDeclaredMethod("GetEntryAssembly").Invoke(null, null);
            var stream = entryAssembly.GetManifestResourceStream(DepsResourceName);

            if (stream == null)
            {
                var path = Path.Combine(entryAssembly.Location, Path.GetFileNameWithoutExtension(entryAssembly.Location) + DepsExtension);
                stream = File.OpenRead(path);
            }

            using (stream)
            {
                return Load(stream);
            }
        }

        public static DependencyContext Load(Stream stream)
        {
            return new DependencyContextReader().Read(stream);
        }
    }
}
