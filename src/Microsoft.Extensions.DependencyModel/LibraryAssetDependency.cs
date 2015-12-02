// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Extensions.DependencyModel
{
    public class LibraryAssetDependency
    {
        public LibraryAssetDependency(string libraryType, string assemblyName, string packageName, string version, string assetType, string hash, string path)
        {
            LibraryType = libraryType;
            AssemblyName = assemblyName;
            PackageName = packageName;
            Version = version;
            AssetType = assetType;
            Hash = hash;
            Path = path;
        }

        public string LibraryType { get; }

        public string AssemblyName { get; }

        public string PackageName { get; }

        public string Version { get; }

        public string AssetType { get; }

        public string Hash { get; }

        public string Path { get; }
    }
}