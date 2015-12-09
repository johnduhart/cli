// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Linq;

namespace Microsoft.DotNet.ProjectModel.Server.Models
{
    public interface IConvertablePayload
    {
        JToken ToJToken(int protocolVersion);
    }
}
