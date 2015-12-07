// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Extensions.DependencyModel
{
    public class Dependency
    {
        public Dependency(string name, string version)
        {
            Name = name;
            Version = version;
        }

        public string Name { get; }
        public string Version { get; }
    }

    public class Library
    {
        public string LibraryType { get; }

        public string PackageName { get; }

        public string Version { get; }

        public string Hash { get; }

        public string[] Assemblies { get; }

        public Dependency[] Dependencies { get; }
    }
}