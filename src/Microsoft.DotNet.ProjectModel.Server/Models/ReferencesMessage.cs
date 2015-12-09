// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.ProjectModel.Server.Models
{
    public class ReferencesMessage
    {
        public FrameworkData Framework { get; set; }
        public IList<string> FileReferences { get; set; }
        public IList<ProjectReference> ProjectReferences { get; set; }

        public override bool Equals(object obj)
        {
            var other = obj as ReferencesMessage;
            return other != null &&
                   Equals(Framework, other.Framework) &&
                   Enumerable.SequenceEqual(ProjectReferences, other.ProjectReferences) &&
                   Enumerable.SequenceEqual(FileReferences, other.FileReferences);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
