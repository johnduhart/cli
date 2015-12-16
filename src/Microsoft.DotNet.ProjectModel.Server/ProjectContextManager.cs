// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.DotNet.ProjectModel.Resolution;
using Microsoft.DotNet.ProjectModel.Server.Helpers;
using Microsoft.DotNet.ProjectModel.Server.InternalModels;
using Microsoft.DotNet.ProjectModel.Server.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using NuGet.Frameworks;

namespace Microsoft.DotNet.ProjectModel.Server
{
    public class ProjectContextManager
    {
        private readonly ILogger _log;

        private readonly object _processingLock = new object();
        private readonly Queue<Message> _inbox = new Queue<Message>();
        private readonly ProjectStateResolver _projectStateResolver;
        private readonly List<ConnectionContext> _waitingForDiagnostics = new List<ConnectionContext>();
        private readonly ProtocolManager _protocolManager;

        private ConnectionContext _initializedContext;

        // triggers
        private readonly Trigger<string> _appPath = new Trigger<string>();
        private readonly Trigger<string> _configure = new Trigger<string>();
        private readonly Trigger<int> _refreshDependencies = new Trigger<int>();
        private readonly Trigger<int> _filesChanged = new Trigger<int>();

        private Snapshot _local = new Snapshot();
        private Snapshot _remote = new Snapshot();
        private readonly WorkspaceContext _workspaceContext;
        private int? _contextProtocolVersion;

        public ProjectContextManager(int contextId,
                                     ILoggerFactory loggerFactory,
                                     WorkspaceContext workspaceContext,
                                     ProtocolManager protocolManager)
        {
            Id = contextId;
            _log = loggerFactory.CreateLogger<ProjectContextManager>();
            _workspaceContext = workspaceContext;
            _projectStateResolver = new ProjectStateResolver(_workspaceContext);
            _protocolManager = protocolManager;
        }

        public int Id { get; }

        public string ProjectPath { get { return _appPath.Value; } }

        public int ProtocolVersion
        {
            get
            {
                if (_contextProtocolVersion.HasValue)
                {
                    return _contextProtocolVersion.Value;
                }
                else
                {
                    return _protocolManager.CurrentVersion;
                }
            }
        }

        public void OnReceive(Message message)
        {
            lock (_inbox)
            {
                _inbox.Enqueue(message);
            }

            ThreadPool.QueueUserWorkItem(state => ((ProjectContextManager)state).ProcessLoop(), this);
        }

        private void ProcessLoop()
        {
            if (!Monitor.TryEnter(_processingLock))
            {
                return;
            }

            try
            {
                lock (_inbox)
                {
                    if (!_inbox.Any())
                    {
                        return;
                    }
                }

                DoProcessLoop();
            }
            catch (Exception ex)
            {
                // TODO: review error handing logic

                _log.LogError($"Error occurred: {ex}");

                var error = new ErrorMessage
                {
                    Message = ex.Message
                };

                var fileFormatException = ex as FileFormatException;
                if (fileFormatException != null)
                {
                    error.Path = fileFormatException.Path;
                    error.Line = fileFormatException.Line;
                    error.Column = fileFormatException.Column;
                }

                var message = new Message
                {
                    ContextId = Id,
                    MessageType = MessageTypes.Error,
                    Payload = JToken.FromObject(error)
                };

                _initializedContext.Transmit(message);
                _remote.GlobalErrorMessage = error;

                foreach (var connection in _waitingForDiagnostics)
                {
                    connection.Transmit(message);
                }

                _waitingForDiagnostics.Clear();
            }
        }

        private void DoProcessLoop()
        {
            while (true)
            {
                DrainInbox();

                var allDiagnostics = new List<DiagnosticsListMessage>();

                ResolveDependencies();
                SendOutgingMessages(allDiagnostics);
                SendDiagnostics(allDiagnostics);

                lock (_inbox)
                {
                    if (_inbox.Count == 0)
                    {
                        return;
                    }
                }
            }
        }

        private void DrainInbox()
        {
            _log.LogInformation("Begin draining inbox.");

            while (ProcessMessage()) { }

            _log.LogInformation("Finish draining inbox.");
        }

        private bool ProcessMessage()
        {
            Message message;

            lock (_inbox)
            {
                if (!_inbox.Any())
                {
                    return false;
                }

                message = _inbox.Dequeue();
                Debug.Assert(message != null);
            }

            _log.LogInformation($"Received {message.MessageType}");

            switch (message.MessageType)
            {
                case MessageTypes.Initialize:
                    Initialize(message);
                    break;
                case MessageTypes.ChangeConfiguration:
                    // TODO: what if the payload is null or represent empty string?
                    _configure.Value = GetValue(message.Payload, "Configuration");
                    break;
                case MessageTypes.RefreshDependencies:
                case MessageTypes.RestoreComplete:
                    _refreshDependencies.Value = 0;
                    break;
                case MessageTypes.FilesChanged:
                    _filesChanged.Value = 0;
                    break;
                case MessageTypes.GetDiagnostics:
                    _waitingForDiagnostics.Add(message.Sender);
                    break;
            }

            return true;
        }

        private void Initialize(Message message)
        {
            if (_initializedContext != null)
            {
                _log.LogWarning($"Received {message.MessageType} message more than once for {_appPath.Value}");
                return;
            }

            _initializedContext = message.Sender;
            _appPath.Value = GetValue(message.Payload, "ProjectFolder");
            _configure.Value = GetValue(message.Payload, "Configuration") ?? "Debug";

            var version = GetValue<int>(message.Payload, "Version");
            if (version != 0 && !_protocolManager.EnvironmentOverridden)
            {
                _contextProtocolVersion = Math.Min(version, _protocolManager.MaxVersion);
                _log.LogInformation($"Set context protocol version to {_contextProtocolVersion.Value}");
            }
        }

        private bool ResolveDependencies()
        {
            ProjectState state = null;

            if (_appPath.WasAssigned || _configure.WasAssigned || _filesChanged.WasAssigned || _refreshDependencies.WasAssigned)
            {
                bool triggerDependencies = _refreshDependencies.WasAssigned;

                _appPath.ClearAssigned();
                _configure.ClearAssigned();
                _filesChanged.ClearAssigned();
                _refreshDependencies.ClearAssigned();

                state = _projectStateResolver.Resolve(_appPath.Value, _configure.Value, triggerDependencies, _remote.ProjectInformation?.ProjectSearchPaths);
            }

            if (state == null)
            {
                return false;
            }


            var frameworkReferenceResolver = FrameworkReferenceResolver.Default;

            _local = new Snapshot();
            _local.Project = state.Project;
            _local.ProjectInformation = ProjectInformation.FromProjectState(state, frameworkReferenceResolver);
            _local.ProjectDiagnostics = new DiagnosticsListMessage(state.Diagnostics);

            foreach (var project in state.Projects)
            {
                var targetFrameworkData = project.Framework.ToPayload(frameworkReferenceResolver);
                var projectWorkd = new ProjectWorld
                {
                    TargetFramework = project.Framework,
                    Sources = new SourceFileReferences
                    {
                        Framework = targetFrameworkData,
                        Files = project.SourceFiles
                    },
                    CompilerOptions = new CompilationOptions
                    {
                        Framework = targetFrameworkData,
                        Options = project.CompilationSettings
                    },
                    Dependencies = new DependenciesMessage
                    {
                        Framework = targetFrameworkData,
                        RootDependency = state.Project.Name,
                        Dependencies = project.DependencyInfo.Dependencies
                    },
                    References = new ReferencesMessage
                    {
                        Framework = targetFrameworkData,
                        ProjectReferences = project.DependencyInfo.ProjectReferences,
                        FileReferences = project.DependencyInfo.References
                    },
                    DependencyDiagnostics = new DiagnosticsListMessage(project.DependencyInfo.Diagnostics,
                                                                       targetFrameworkData)
                };

                _local.Projects[project.Framework] = projectWorkd;
            }

            return true;
        }

        private void SendOutgingMessages(List<DiagnosticsListMessage> diagnostics)
        {
            ComparePropertyAndSend(_local, _remote, nameof(_local.ProjectInformation), MessageTypes.ProjectInformation);

            if (_local.ProjectDiagnostics != null)
            {
                diagnostics.Add(_local.ProjectDiagnostics);
            }

            ComparePropertyAndSend(_local, _remote, nameof(_local.ProjectDiagnostics), MessageTypes.Diagnostics);

            var unprocessedFrameworks = new HashSet<NuGetFramework>(_remote.Projects.Keys);
            foreach (var pair in _local.Projects)
            {
                ProjectWorld localProject = pair.Value;
                ProjectWorld remoteProject;

                if (!_remote.Projects.TryGetValue(pair.Key, out remoteProject))
                {
                    remoteProject = new ProjectWorld();
                    _remote.Projects[pair.Key] = remoteProject;
                }

                if (localProject.DependencyDiagnostics != null)
                {
                    diagnostics.Add(localProject.DependencyDiagnostics);
                }

                unprocessedFrameworks.Remove(pair.Key);

                ComparePropertyAndSend(localProject, remoteProject, nameof(localProject.DependencyDiagnostics), MessageTypes.DependencyDiagnostics);
                ComparePropertyAndSend(localProject, remoteProject, nameof(localProject.Dependencies), MessageTypes.Dependencies);
                ComparePropertyAndSend(localProject, remoteProject, nameof(localProject.CompilerOptions), MessageTypes.CompilerOptions);
                ComparePropertyAndSend(localProject, remoteProject, nameof(localProject.References), MessageTypes.References);
                ComparePropertyAndSend(localProject, remoteProject, nameof(localProject.Sources), MessageTypes.Sources);
            }

            // Remove all processed frameworks from the remote view
            foreach (var framework in unprocessedFrameworks)
            {
                _remote.Projects.Remove(framework);
            }
        }

        private void SendDiagnostics(List<DiagnosticsListMessage> allDiagnostics)
        {
            _log.LogInformation($"SendDiagnostics, {allDiagnostics.Count()} diagnostics, {_waitingForDiagnostics.Count()} waiting for diagnostics.");
            if (!allDiagnostics.Any())
            {
                return;
            }

            ComparePropertyAndSend(_local, _remote, nameof(_local.GlobalErrorMessage), MessageTypes.Error, _waitingForDiagnostics);

            // Group all of the diagnostics into group by target framework
            var messages = new List<DiagnosticsListMessage>();
            foreach (var g in allDiagnostics.GroupBy(g => g.Framework))
            {
                var messageGroup = g.SelectMany(d => d.Diagnostics).ToList();
                messages.Add(new DiagnosticsListMessage(messageGroup, g.Key));
            }

            // Send all diagnostics back
            var payload = JToken.FromObject(messages.Select(d => d.ToJToken(ProtocolVersion)));
            var message = Message.FromPayload(MessageTypes.AllDiagnostics, Id, ProtocolVersion, payload);
            foreach (var connection in _waitingForDiagnostics)
            {
                connection.Transmit(message);
            }

            _waitingForDiagnostics.Clear();
        }

        private void ComparePropertyAndSend<T>(T local, T remote, string propertyName, string messageType, IEnumerable<ConnectionContext> additionalConnections)
        {
            var property = typeof(T).GetProperty(propertyName);
            if (property == null)
            {
                _log.LogError($"Property {propertyName} is missing.");
                return;
            }

            var localProperty = property.GetValue(local);
            var remoteProperty = property.GetValue(remote);

            if (IsDifferent(localProperty, remoteProperty))
            {
                _log.LogInformation($"Sending {messageType}");

                var message = Message.FromPayload(messageType, Id, ProtocolVersion, localProperty);
                _initializedContext.Transmit(message);
                foreach (var connection in additionalConnections)
                {
                    connection.Transmit(message);
                }

                property.SetValue(remote, localProperty);
            }
        }

        private void ComparePropertyAndSend<T>(T local, T remote, string propertyName, string messageType)
        {
            ComparePropertyAndSend(local, remote, propertyName, messageType, Enumerable.Empty<ConnectionContext>());
        }

        private static string GetValue(JToken token, string name)
        {
            return GetValue<string>(token, name);
        }

        private static TVal GetValue<TVal>(JToken token, string name)
        {
            var value = token?[name];
            if (value != null)
            {
                return value.Value<TVal>();
            }

            return default(TVal);
        }

        private static bool IsDifferent<T>(T local, T remote) where T : class
        {
            // If no value was ever produced, then don't even bother
            if (local == null)
            {
                return false;
            }

            return !Equals(local, remote);
        }

        private class Trigger<TValue>
        {
            private TValue _value;

            public bool WasAssigned { get; private set; }

            public void ClearAssigned()
            {
                WasAssigned = false;
            }

            public TValue Value
            {
                get { return _value; }
                set
                {
                    WasAssigned = true;
                    _value = value;
                }
            }
        }
    }
}
