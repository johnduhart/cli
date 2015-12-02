// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Extensions.DependencyModel
{
    public class Dependency
    {
        public Dependency(string assemblyName, string packageName, string version, string checksum)
        {
            AssemblyName = assemblyName;
            PackageName = packageName;
            Version = version;
            Checksum = checksum;
        }

        public string AssemblyName { get; }

        public string PackageName { get; }

        public string Version { get; }

        public string Checksum { get; }
    }
}