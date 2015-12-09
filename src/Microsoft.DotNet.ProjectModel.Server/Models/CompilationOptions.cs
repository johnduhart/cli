// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.ProjectModel.Server.Models
{
    public class CompilationOptions
    {
        public FrameworkData Framework { get; set; }

        public CommonCompilerOptions Options { get; set; }

        public override bool Equals(object obj)
        {
            var other = obj as CompilationOptions;
            return other != null &&
                   Equals(Framework, other.Framework) &&
                   Equals(Options, other.Options);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
