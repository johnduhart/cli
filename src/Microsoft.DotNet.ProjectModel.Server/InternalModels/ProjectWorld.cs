// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.ProjectModel.Server.Models;
using NuGet.Frameworks;

namespace Microsoft.DotNet.ProjectModel.Server.InternalModels
{
    public class ProjectWorld
    {
        // State
        public NuGetFramework TargetFramework { get; set; }

        // Payloads
        public SourceFileReferences Sources { get; set; }
        public ReferencesMessage References { get; set; }
        public DependenciesMessage Dependencies { get; set; }
        public DiagnosticsListMessage DependencyDiagnostics { get; set; }
        public CompilationOptions CompilerOptions { get; set; }
    }
}
