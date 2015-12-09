// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.DotNet.ProjectModel.Compilation;
using Microsoft.DotNet.ProjectModel.Graph;
using Microsoft.DotNet.ProjectModel.Server.Helpers;
using Microsoft.DotNet.ProjectModel.Server.InternalModels;
using Microsoft.DotNet.ProjectModel.Server.Models;

namespace Microsoft.DotNet.ProjectModel.Server
{
    internal class ProjectStateResolver
    {
        private readonly WorkspaceContext _workspaceContext;

        public ProjectStateResolver(WorkspaceContext workspaceContext)
        {
            _workspaceContext = workspaceContext;
        }

        public ProjectState Resolve(string appPath,
                                    string configuration,
                                    bool triggerDependencies,
                                    IList<string> currentSearchPaths)
        {

            var projectContextsCollection = _workspaceContext.GetProjectContextsCollection(appPath);
            if (!projectContextsCollection.ProjectContexts.Any())
            {
                throw new InvalidOperationException($"Unable to find project.json in '{appPath}'");
            }

            var state = new ProjectState(projectContextsCollection);
            var project = state.Project;

            var sourcesProjectWidesources = project.Files.SourceFiles.ToList();
            var projectSearchPaths = project.ResolveSearchPaths();

            foreach (var projectContext in projectContextsCollection.ProjectContexts)
            {
                var dependencyInfo = ResolveProjectDependencies(projectContext,
                                                                configuration,
                                                                GetUpdatedSearchPaths(currentSearchPaths, projectSearchPaths));

                var dependencySources = new List<string>(sourcesProjectWidesources);

                var framework = projectContext.TargetFramework;

                // Add shared files from packages
                dependencySources.AddRange(dependencyInfo.ExportedSourcesFiles);

                // Add shared files from projects
                foreach (var reference in dependencyInfo.ProjectReferences)
                {
                    if (reference.Project == null)
                    {
                        continue;
                    }

                    // Only add direct dependencies as sources
                    if (!project.Dependencies.Any(d => string.Equals(d.Name, reference.Name, StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }

                    dependencySources.AddRange(reference.Project.Files.SharedFiles);
                }

                var projectInfo = new ProjectInfo
                {
                    Framework = framework,
                    CompilationSettings = project.GetCompilerOptions(framework, configuration),
                    SourceFiles = dependencySources,
                    DependencyInfo = dependencyInfo
                };

                state.Projects.Add(projectInfo);
            }

            return state;
        }

        private DependencyInfo ResolveProjectDependencies(ProjectContext projectContext,
                                                          string configuration,
                                                          IEnumerable<string> updatedSearchPath)
        {
            var libraryManager = projectContext.LibraryManager;
            var libraryExporter = projectContext.CreateExporter(configuration);

            var info = new DependencyInfo
            {
                Dependencies = new Dictionary<string, DependencyDescription>(),
                ProjectReferences = new List<ProjectReference>(),
                References = new List<string>(),
                ExportedSourcesFiles = new List<string>(),
                Diagnostics = libraryManager.GetAllDiagnostics().ToList()
            };

            var diagnosticSources = info.Diagnostics.ToLookup(diagnostic => diagnostic.Source);
            var projectCandiates = GetProjectCandidates(updatedSearchPath);

            var allLibraries = libraryManager.GetLibraries();
            var rootDependencies = allLibraries.FirstOrDefault(library => string.Equals(library.Identity.Name, projectContext.ProjectFile.Name))
                                              ?.Dependencies
                                              ?.ToDictionary(libraryRange => libraryRange.Name);

            var allExports = libraryExporter.GetAllExports().ToDictionary(export => export.Library.Identity);

            foreach (var library in allLibraries)
            {
                var diagnostics = diagnosticSources[library].ToList();

                var newDiagnostic = ValidateDependency(library, projectCandiates, rootDependencies);
                if (newDiagnostic != null)
                {
                    info.Diagnostics.Add(newDiagnostic);
                    diagnostics.Add(newDiagnostic);
                }

                var description = CreateDependencyDescription(library, diagnostics);

                info.Dependencies[description.Name] = description;

                if (string.Equals(library.Identity.Type, LibraryType.Project) &&
                   !string.Equals(library.Identity.Name, projectContext.ProjectFile.Name))
                {
                    var referencedProject = (ProjectDescription)library;

                    var targetFrameworkInformation = referencedProject.TargetFrameworkInfo;

                    // If this is an assembly reference then treat it like a file reference
                    if (!string.IsNullOrEmpty(targetFrameworkInformation?.AssemblyPath) &&
                         string.IsNullOrEmpty(targetFrameworkInformation?.WrappedProject))
                    {
                        string assemblyPath = GetProjectRelativeFullPath(referencedProject.Project,
                                                                         targetFrameworkInformation.AssemblyPath);
                        info.References.Add(assemblyPath);

                        description.Path = assemblyPath;
                        description.Type = "Assembly";
                    }
                    else
                    {
                        string wrappedProjectPath = null;

                        if (!string.IsNullOrEmpty(targetFrameworkInformation?.WrappedProject) &&
                            referencedProject.Project != null)
                        {
                            wrappedProjectPath = GetProjectRelativeFullPath(referencedProject.Project, targetFrameworkInformation.WrappedProject);
                        }

                        info.ProjectReferences.Add(new ProjectReference
                        {
                            Name = referencedProject.Identity.Name,
                            Framework = new FrameworkData
                            {
                                ShortName = library.Framework.GetShortFolderName(),
                                FrameworkName = library.Framework.DotNetFrameworkName,
                                FriendlyName = library.Framework.Framework
                            },
                            Path = library.Path,
                            WrappedProjectPath = wrappedProjectPath,
                            Project = referencedProject.Project
                        });
                    }
                }

                if (library.Identity.Type != LibraryType.Project)
                {
                    LibraryExport export;
                    if (allExports.TryGetValue(library.Identity, out export))
                    {
                        info.References.AddRange(export.CompilationAssemblies.Select(asset => asset.ResolvedPath));
                        info.ExportedSourcesFiles.AddRange(export.SourceReferences);
                    }
                }
            }

            return info;
        }

        private DiagnosticMessage ValidateDependency(LibraryDescription library,
                                                     HashSet<string> projectCandidates,
                                                     Dictionary<string, LibraryRange> rootDependencies)
        {
            if (!library.Resolved || projectCandidates == null)
            {
                return null;
            }

            var foundCandidate = projectCandidates.Contains(library.Identity.Name);

            if ((library.Identity.Type == LibraryType.Project && !foundCandidate) ||
                (library.Identity.Type == LibraryType.Package && foundCandidate))
            {
                library.Resolved = false;

                var libraryRange = rootDependencies[library.Identity.Name];

                return new DiagnosticMessage(
                    ErrorCodes.NU1010,
                    $"The type of dependency {library.Identity.Name} was changed.",
                    libraryRange.SourceFilePath,
                    DiagnosticMessageSeverity.Error,
                    libraryRange.SourceLine,
                    libraryRange.SourceColumn,
                    library);
            }

            return null;
        }

        private static HashSet<string> GetProjectCandidates(IEnumerable<string> searchPaths)
        {
            if (searchPaths == null)
            {
                return null;
            }

            return new HashSet<string>(searchPaths.Where(path => Directory.Exists(path))
                                                  .SelectMany(path => Directory.GetDirectories(path))
                                                  .Where(path => File.Exists(Path.Combine(path, Project.FileName)))
                                                  .Select(path => Path.GetFileName(path)));
        }

        private static DependencyDescription CreateDependencyDescription(LibraryDescription library,
                                                                                IEnumerable<DiagnosticMessage> diagnostics)
        {
            var result = new DependencyDescription
            {
                Name = library.Identity.Name,
                DisplayName = GetLibraryDisplayName(library),
                Version = library.Identity.Version?.ToString(),
                Type = library.Identity.Type.Value,
                Resolved = library.Resolved,
                Path = library.Path,
                Dependencies = library.Dependencies.Select(dependency => new DependencyItem
                {
                    Name = dependency.Name,
                    Version = dependency.VersionRange?.ToString() // TODO: review
                }),
                Errors = diagnostics.Where(d => d.Severity == DiagnosticMessageSeverity.Error)
                                    .Select(d => new DiagnosticMessageView(d)),
                Warnings = diagnostics.Where(d => d.Severity == DiagnosticMessageSeverity.Warning)
                                      .Select(d => new DiagnosticMessageView(d))
            };

            return result;
        }

        private static string GetLibraryDisplayName(LibraryDescription library)
        {
            var name = library.Identity.Name;
            if (library.Identity.Type == LibraryType.ReferenceAssembly && name.StartsWith("fx/"))
            {
                name = name.Substring(3);
            }

            return name;
        }

        private static string GetProjectRelativeFullPath(Project referencedProject, string path)
        {
            return Path.GetFullPath(Path.Combine(referencedProject.ProjectDirectory, path));
        }

        /// <summary>
        /// Returns the search paths if they're updated. Otherwise returns null.
        /// </summary>
        private static IEnumerable<string> GetUpdatedSearchPaths(IEnumerable<string> oldSearchPaths,
                                                                 IEnumerable<string> newSearchPaths)
        {
            // The oldSearchPaths is null when the current project is not initialized. It is not necessary to 
            // validate the dependency in this case.
            if (oldSearchPaths == null)
            {
                return null;
            }

            if (Enumerable.SequenceEqual(oldSearchPaths, newSearchPaths))
            {
                return null;
            }

            return newSearchPaths;
        }
    }
}
