/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

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
    /// // Listen for subscribe requests...
    /// // - The handler must return an IDisposable object (gets disposed when the channel is unsubscribed)
    /// // - The handler can push data to the client by calling channel.Queue()
    /// subscriptionApi.OnSubscribe("todos", (vars, channel) => {
    ///     return database.CreateAndStartDynamicView("todo", dataEventTransaction => channel.Queue(dataEventTransaction));
    /// });
    /// subscriptionApi.Start();
    /// </code>
    /// </remarks>
    public interface ISubscriptionApi : IDisposable {

        /// <summary>
        /// Define a channel that clients can subscribe.  Can specify a <paramref name="handler"/> or a <paramref name="handlerAsync"/> (but not both) that is invoked when a client subscribes.
        /// </summary>
        /// <param name="channelKey">Unique identifier for the channel</param>
        /// <param name="handler">Code to run to create subscription. Must return an IDisposable instance that is disposed on unsubscribe. Can push data to client by calling <see cref="Channel.Queue(object)"/></param>
        /// <param name="handlerAsync">Async code to run to create subscription. Must return an IDisposable instance that is disposed on unsubscribe. Can push data to client by calling <see cref="Channel.Queue(object)"/></param>
        /// <returns></returns>
        ChannelSubscription OnSubscribe(string channelKey, Func<Dict, Channel, IDisposable> handler = null, Func<Dict, Channel, Task<IDisposable>> handlerAsync = null);

        /// <summary>
        /// Get all the unauthenticted connections
        /// </summary>
        ICollection<IChannelConnection> UnauthenticatedConnections { get; }

        /// <summary>
        /// Get all the authenticted connections
        /// </summary>
        ICollection<IChannelConnection> AuthenticatedConnections { get; }

        /// <summary>
        /// Retrieve a channel by id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="throwExceptionIfMissing"></param>
        /// <returns></returns>
        IChannelConnection GetConnection(string id, bool throwExceptionIfMissing = false);

        /// <summary>
        /// Starts the channel server
        /// </summary>
        void Start();
    }
}
