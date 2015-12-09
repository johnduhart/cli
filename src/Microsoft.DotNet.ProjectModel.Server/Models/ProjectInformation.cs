// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.ProjectModel.Resolution;
using Microsoft.DotNet.ProjectModel.Server.Helpers;
using Microsoft.DotNet.ProjectModel.Server.InternalModels;

namespace Microsoft.DotNet.ProjectModel.Server.Models
{
    public class ProjectInformation
    {
        private ProjectInformation(string name, 
                                   List<FrameworkData> frameworks,
                                   List<string> configurations,
                                   IDictionary<string, string> commands,
                                   List<string> projectSearchPaths,
                                   string globalJsonPath)
        {
            Name = name;
            Frameworks = frameworks;
            Configurations = configurations;
            Commands = commands;
            ProjectSearchPaths = projectSearchPaths;
            GlobalJsonPath = globalJsonPath;
        }

        internal static ProjectInformation FromProjectState(ProjectState state, FrameworkReferenceResolver resolver)
        {
            GlobalSettings globalSettings;
            var projectSearchPaths = state.Project.ResolveSearchPaths(out globalSettings);
            var project = state.Project;

            return new ProjectInformation(
                name: project.Name,
                frameworks: state.Projects.Select(pi => pi.Framework.ToPayload(resolver)).ToList(),
                configurations: project.GetConfigurations().ToList(),
                commands: state.Project.Commands,
                projectSearchPaths: projectSearchPaths.ToList(),
                globalJsonPath: globalSettings?.FilePath);
        }

        public string Name { get; }

        public IList<FrameworkData> Frameworks { get; }

        public IList<string> Configurations { get; }

        public IDictionary<string, string> Commands { get; }

        public IList<string> ProjectSearchPaths { get; }

        public string GlobalJsonPath { get; }

        public override bool Equals(object obj)
        {
            var other = obj as ProjectInformation;

            return other != null &&
                   string.Equals(Name, other.Name) &&
                   string.Equals(GlobalJsonPath, other.GlobalJsonPath) &&
                   Enumerable.SequenceEqual(Frameworks, other.Frameworks) &&
                   Enumerable.SequenceEqual(Configurations, other.Configurations) &&
                   Enumerable.SequenceEqual(Commands, other.Commands) &&
                   Enumerable.SequenceEqual(ProjectSearchPaths, other.ProjectSearchPaths);
        }

        public override int GetHashCode()
        {
            // These objects are currently POCOs and we're overriding equals
            // so that things like Enumerable.SequenceEqual just work.
            return base.GetHashCode();
        }
    }
}
