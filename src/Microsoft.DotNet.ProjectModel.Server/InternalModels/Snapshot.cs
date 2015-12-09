// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.DotNet.ProjectModel.Server.InternalModels;
using Microsoft.DotNet.ProjectModel.Server.Models;
using NuGet.Frameworks;

namespace Microsoft.DotNet.ProjectModel.Server
{
    public class Snapshot
    {
        public Dictionary<NuGetFramework, ProjectWorld> Projects { get; set; } = new Dictionary<NuGetFramework, ProjectWorld>();

        // Payloads
        public Project Project { get; set; }

        public ProjectInformation ProjectInformation { get; set; }

        public DiagnosticsListMessage ProjectDiagnostics { get; set; }
        
        public ErrorMessage GlobalErrorMessage { get; set; } = new ErrorMessage();
    }
}
