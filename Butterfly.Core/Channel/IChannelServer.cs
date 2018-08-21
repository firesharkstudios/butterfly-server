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

        /// <summary>
        /// Define a channel that clients can subscribe.  Can specify a <paramref name="handler"/> or a <paramref name="handlerAsync"/> (but not both) that is invoked when a client subscribes.
        /// </summary>
        /// <param name="channelKey"></param>
        /// <param name="handler">Code to run to create subscription. Must return an IDisposable instance that is disposed on unsubscribe. Can push data to client by calling <see cref="Channel.Queue(object)"/></param>
        /// <param name="handlerAsync">Async code to run to create subscription. Must return an IDisposable instance that is disposed on unsubscribe. Can push data to client by calling <see cref="Channel.Queue(object)"/></param>
        /// <returns></returns>
        ChannelSubscription OnSubscribe(string channelKey, Func<Dict, Channel, IDisposable> handler = null, Func<Dict, Channel, Task<IDisposable>> handlerAsync = null);

        /// <summary>
        /// Get all the unauthenticted connections
        /// </summary>
        ICollection<IChannelServerConnection> UnauthenticatedConnections { get; }

        /// <summary>
        /// Get all the authenticted connections
        /// </summary>
        ICollection<IChannelServerConnection> AuthenticatedConnections { get; }

        /// <summary>
        /// Retrieve a channel by id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="throwExceptionIfMissing"></param>
        /// <returns></returns>
        IChannelServerConnection GetConnection(string id, bool throwExceptionIfMissing = false);

        /// <summary>
        /// Starts the channel server
        /// </summary>
        void Start();
    }
}
