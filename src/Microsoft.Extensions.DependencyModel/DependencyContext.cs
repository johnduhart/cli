// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System;

namespace Microsoft.Extensions.DependencyModel
{
    public class DependencyContext
    {
        public IList<Library> RuntimeLibraries { get; }
        public IList<Library> CompilationLibraries { get; }

        private const string DepsExtension = ".deps";

        public DependencyContext()
        {
        }

        public DependencyContext(IList<Library> runtimeLibraries, IList<Library> compilationLibraries)
        {
            RuntimeLibraries = runtimeLibraries;
            CompilationLibraries = compilationLibraries;
        }

        public static DependencyContext Load()
        {
            Assembly entryAssembly = null;
            using (var fileStream = File.OpenRead(entryAssembly.Location))
            {
                return Load(fileStream);
            }
        }

        private static DependencyContext Load(Stream stream)
        {
            return new DependencyContext();
        }
    }
}
