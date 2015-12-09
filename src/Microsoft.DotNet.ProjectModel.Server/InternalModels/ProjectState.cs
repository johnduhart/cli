// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Collections.Generic;

namespace Microsoft.DotNet.ProjectModel.Server.InternalModels
{
    internal class ProjectState
    {
        public ProjectState(ProjectContextsCollection contextsCollection)
        {
            Project = contextsCollection.ProjectContexts.First().ProjectFile;
            Diagnostics = new List<DiagnosticMessage>(contextsCollection.ProjectDiagnostics);
            Projects = new List<ProjectInfo>();
        }

        public Project Project { get; }

        public List<ProjectInfo> Projects { get; }

        public IReadOnlyList<DiagnosticMessage> Diagnostics { get; }
    }
}
