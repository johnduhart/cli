// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.ProjectModel.Server.Models
{
    public class SourceFileReferences
    {
        public FrameworkData Framework { get; set; }
        public IList<string> Files { get; set; }
        public IDictionary<string, string> GeneratedFiles { get; set; }

        public override bool Equals(object obj)
        {
            var other = obj as SourceFileReferences;
            return other != null && Enumerable.SequenceEqual(Files, other.Files);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
