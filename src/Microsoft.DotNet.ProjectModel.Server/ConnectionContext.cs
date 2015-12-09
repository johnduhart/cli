// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using Microsoft.DotNet.ProjectModel.Resolution;
using Microsoft.DotNet.ProjectModel.Server.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Microsoft.DotNet.ProjectModel.Server
{
    public class ConnectionContext
    {
        private readonly string _hostName;
        private readonly ProcessingQueue _queue;
        private readonly IDictionary<int, ApplicationContext> _applicationContexts;

        public ConnectionContext(string hostName,
                                 Socket acceptedSocket,
                                 ProtocolManager protocolManager,
                                 FrameworkReferenceResolver frameworkReferenceResolver,
                                 WorkspaceContext workspaceContext,
                                 IDictionary<int, ApplicationContext> keepers,
                                 ILoggerFactory loggerFactory)
        {
            _hostName = hostName;
            _applicationContexts = keepers;

            _queue = new ProcessingQueue(new NetworkStream(acceptedSocket), loggerFactory);
            _queue.OnReceive += message =>
            {
                // Enumerates all project contexts and return them to the
                // sender
                if (message.MessageType == MessageTypes.EnumerateProjectContexts)
                {
                    WriteProjectContexts();
                }
                else if (protocolManager.IsProtocolNegotiation(message))
                {
                    message.Sender = this;
                    protocolManager.Negotiate(message);
                }
                else
                {
                    message.Sender = this;
                    ApplicationContext keeper;
                    if (!_applicationContexts.TryGetValue(message.ContextId, out keeper))
                    {
                        keeper = new ApplicationContext(message.ContextId,
                                                        loggerFactory,
                                                        workspaceContext,
                                                        protocolManager,
                                                        frameworkReferenceResolver);

                        _applicationContexts[message.ContextId] = keeper;
                    }

                    keeper.OnReceive(message);
                }
            };
        }

        public void QueueStart()
        {
            _queue.Start();
        }

        public bool Transmit(Message message)
        {
            message.HostId = _hostName;
            return _queue.Send(message);
        }

        public bool Transmit(Action<BinaryWriter> writer)
        {
            return _queue.Send(writer);
        }

        private void WriteProjectContexts()
        {
            try
            {
                var response = new
                {
                    MessageType = MessageTypes.ProjectContexts,
                    Projects = _applicationContexts.ToDictionary(
                        pair => pair.Value.ProjectPath,
                        pair => pair.Key)
                };

                _queue.Send(writer => writer.Write(JsonConvert.SerializeObject(response)));
            }
            catch (Exception ex)
            {
                _queue.Send(new Message
                {
                    MessageType = "Error",
                    Payload = JsonConvert.SerializeObject(new { Message = ex.Message })
                });

                throw;
            }
        }
    }
}