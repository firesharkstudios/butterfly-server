/*
 * Copyright 2017 Fireshark Studios, LLC
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;

namespace Butterfly.Channel {
    /// <summary>
    /// Internal interface representing a communications channel from the server to the client (might be implemented via WebSockets, HTTP long polling, etc)
    /// </summary>
    public interface IChannelServerConnection : IDisposable {
        object AuthToken { get; }
        string Id { get; }
        DateTime Created { get;  }
        RegisteredRoute RegisteredRoute { get; }
        DateTime LastHeartbeat { get; }
        void Start(object authToken, string id);
        void QueueChannelMessage(string messageType, string channelKey, object value);
    }
}
