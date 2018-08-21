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
using System.Collections.Generic;
using System.Threading.Tasks;

using Dict = System.Collections.Generic.Dictionary<string, object>;

namespace Butterfly.Core.Channel {
    /// <summary>
    /// Allows clients to subscribe to channels and allows the server to push data to subscribed clients.<para/>
    /// </summary>
    /// <remarks>
    /// Listen for subscription requests to the todos channel...<para/>
    /// <code>
    ///     var channelServer = new SomeChannelServer();
    ///     // Listen for subscribe requests...
    ///     // - The handler must return an IDisposable object (gets disposed when the channel is unsubscribed)
    ///     // - The handler can push data to the client by calling channel.Queue()
    ///     channelServer.OnSubscribe("todos", (vars, channel) => {
    ///         return database.CreateAndStartDynamicView("todo", dataEventTransaction => channel.Queue(dataEventTransaction));
    ///     });
    ///     channelServer.Start();
    /// </code>
    /// </remarks>
    public interface IChannelServer : IDisposable {

        ChannelSubscription OnSubscribe(string channelKey, Func<Dict, Channel, IDisposable> handler = null, Func<Dict, Channel, Task<IDisposable>> handlerAsync = null);

        /// <summary>
        /// Number of connections
        /// </summary>
        ICollection<IChannelServerConnection> UnauthenticatedConnections { get; }

        ICollection<IChannelServerConnection> AuthenticatedConnections { get; }

        /// <summary>
        /// Retrieve a channel by id
        /// </summary>
        /// <param name="authId"></param>
        /// <param name="throwExceptionIfMissing"></param>
        /// <returns></returns>
        IChannelServerConnection GetConnection(string authId, bool throwExceptionIfMissing = false);

        /// <summary>
        /// Starts the channel server
        /// </summary>
        void Start();
    }
}
