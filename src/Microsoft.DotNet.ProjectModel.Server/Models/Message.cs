// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.DotNet.ProjectModel.Server.Models
{
    public class Message
    {
        public static Message FromPayload(string messageType, int contextId, int protocolVersion, object payload)
        {
            if (payload is IConvertablePayload)
            {
                return new Message
                {
                    MessageType = messageType,
                    ContextId = contextId,
                    Payload = ((IConvertablePayload)payload).ToJToken(protocolVersion)
                };
            }
            else
            {
                return new Message
                {
                    MessageType = messageType,
                    ContextId = contextId,
                    Payload = JToken.FromObject(payload)
                };
            }
        }

        public string MessageType { get; set; }

        public string HostId { get; set; }

        public int ContextId { get; set; } = -1;

        public JToken Payload { get; set; }

        [JsonIgnore]
        public ConnectionContext Sender { get; set; }

        public override string ToString()
        {
            return $"({HostId}, {MessageType}, {ContextId}) -> {Payload?.ToString(Formatting.Indented)}";
        }
    }
}
