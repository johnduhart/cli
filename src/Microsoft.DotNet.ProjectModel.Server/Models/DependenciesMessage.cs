// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.ProjectModel.Server.Models
{
    public class DependenciesMessage
    {
        public FrameworkData Framework { get; internal set; }
        public string RootDependency { get; internal set; }
        public IDictionary<string, DependencyDescription> Dependencies { get; internal set; }

        public override bool Equals(object obj)
        {
            var other = obj as DependenciesMessage;
            return other != null &&
                   string.Equals(RootDependency, other.RootDependency) &&
                   object.Equals(Framework, other.Framework) &&
                   Enumerable.SequenceEqual(Dependencies, other.Dependencies);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
