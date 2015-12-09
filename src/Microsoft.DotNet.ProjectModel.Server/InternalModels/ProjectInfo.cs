// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using NuGet.Frameworks;

namespace Microsoft.DotNet.ProjectModel.Server.InternalModels
{
    internal class ProjectInfo
    {
        public NuGetFramework Framework { get; set; }

        public CommonCompilerOptions CompilationSettings { get; set; }

        public List<string> SourceFiles { get; set; }

        public DependencyInfo DependencyInfo { get; set; }
    }
}
