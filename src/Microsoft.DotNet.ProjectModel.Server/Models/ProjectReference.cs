// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.DotNet.ProjectModel.Server.Models
{
    public class ProjectReference
    {
        public FrameworkData Framework { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public string WrappedProjectPath { get; set; }

        [JsonIgnore]
        public Project Project { get; set; }

        public override bool Equals(object obj)
        {
            var other = obj as ProjectReference;
            return other != null &&
                   object.Equals(Framework, other.Framework) &&
                   string.Equals(Name, other.Name) &&
                   string.Equals(Path, other.Path) &&
                   string.Equals(WrappedProjectPath, other.WrappedProjectPath);
        }

        public override int GetHashCode()
        {
            // These objects are currently POCOs and we're overriding equals
            // so that things like Enumerable.SequenceEqual just work.
            return base.GetHashCode();
        }
    }
}
