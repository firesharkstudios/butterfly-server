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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using NLog;

using Butterfly.Core.Util;

using Dict = System.Collections.Generic.Dictionary<string, object>;

namespace Butterfly.Core.Channel {

    /// <inheritdoc/>
    /// <summary>
    /// Base class implementing <see cref="IChannelServer"/>. New implementations will normally extend this class.
    /// </summary>
    public abstract class BaseChannelServer : IChannelServer {
        protected static readonly Logger logger = LogManager.GetCurrentClassLogger();

        protected readonly ConcurrentDictionary<IChannelServerConnection, IChannelServerConnection> unauthenticatedConnections = new ConcurrentDictionary<IChannelServerConnection, IChannelServerConnection>();
        protected readonly ConcurrentDictionary<string, IChannelServerConnection> authenticatedConnectionByAuthId = new ConcurrentDictionary<string, IChannelServerConnection>();

        public readonly Func<string, string, object> getAuthToken;
        public readonly Func<string, string, Task<object>> getAuthTokenAsync;

        public readonly Func<object, string> getId;
        public readonly Func<object, Task<string>> getIdAsync;

        protected readonly Dictionary<string, ChannelSubscription> channelSubscriptionByKey = new Dictionary<string, ChannelSubscription>();

        protected readonly int mustReceiveHeartbeatMillis;

        public BaseChannelServer(int mustReceiveHeartbeatMillis = 5000, Func<string, string, object> getAuthToken = null, Func<string, string, Task<object>> getAuthTokenAsync = null, Func<object, string> getId = null, Func<object, Task<string>> getIdAsync = null) {
            this.mustReceiveHeartbeatMillis = mustReceiveHeartbeatMillis;

            if (getAuthToken != null && getAuthTokenAsync != null) {
                throw new Exception("Can specify getAuthToken or getAuthTokenAsync but not both");
            }
            else if (getAuthToken != null) {
                this.getAuthToken = getAuthToken;
                this.getAuthTokenAsync = null;
            }
            else if (getAuthTokenAsync != null) {
                this.getAuthToken = null;
                this.getAuthTokenAsync = getAuthTokenAsync;
            }
            else {
                this.getAuthToken = (authType, authValue) => authValue;
                this.getAuthTokenAsync = null;
            }

            if (getId != null && getIdAsync != null) {
                throw new Exception("Can specify getId or getIdAsync but not both");
            }
            else if (getId != null) {
                this.getId = getId;
                this.getIdAsync = null;
            }
            else if (getIdAsync != null) {
                this.getId = null;
                this.getIdAsync = getIdAsync;
            }
            else {
                this.getId = authToken => Guid.NewGuid().ToString();
                this.getIdAsync = null;
            }
        }

        public Dictionary<string, ChannelSubscription> ChannelSubscriptionByKey => this.channelSubscriptionByKey;

        public ChannelSubscription OnSubscribe(string channelKey, Func<Dict, Channel, IDisposable> handler = null, Func<Dict, Channel, Task<IDisposable>> handlerAsync = null) {
            if (this.channelSubscriptionByKey.TryGetValue(channelKey, out ChannelSubscription channelSubscription)) throw new Exception($"Already a registered channel '{channelKey}'");
            if (handler != null && handlerAsync != null) {
                throw new Exception("Can only specify a handler or handlerAsync but not both");
            }
            else if (handler != null) {
                channelSubscription = new ChannelSubscription(channelKey, handler);
            }
            else if (handlerAsync != null) {
                channelSubscription = new ChannelSubscription(channelKey, handlerAsync);
            }
            else {
                throw new Exception("Must specify a handler for new channels");
            }
            this.channelSubscriptionByKey[channelKey] = channelSubscription;
            return channelSubscription;
        }

        public void AddUnauthenticatedConnection(IChannelServerConnection connection) {
            //logger.Debug($"AddUnauthenticatedChannel():channel.Path={channel.WebRequest.RequestUri.AbsolutePath}");
            this.unauthenticatedConnections[connection] = connection;
        }


        internal async Task AuthenticateAsync(string authType, string authValue, BaseChannelServerConnection connection) {
            logger.Debug($"AuthenticateAsync():authType={authType},authValue={authValue}");

            this.unauthenticatedConnections.TryRemove(connection, out IChannelServerConnection dummyChannel);

            object authToken = this.getAuthToken != null ? this.getAuthToken(authType, authValue) : await this.getAuthTokenAsync(authType, authValue);
            string id = this.getId != null ? this.getId(authToken) : await this.getIdAsync(authToken);
            if (string.IsNullOrEmpty(id)) {
                throw new Exception("Could not create id for connection");
            }
            else {
                var existingConnection = this.GetConnection(id);
                if (existingConnection != null) {
                    existingConnection.Dispose();
                }
                connection.Start(authToken, id);
                this.authenticatedConnectionByAuthId[id] = connection;
            }
        }

        public ICollection<IChannelServerConnection> UnauthenticatedConnections => this.unauthenticatedConnections.Values;

        public ICollection<IChannelServerConnection> AuthenticatedConnections => this.authenticatedConnectionByAuthId.Values;

        public IChannelServerConnection GetConnection(string authId, bool throwExceptionIfMissing = false) {
            if (!this.authenticatedConnectionByAuthId.TryGetValue(authId, out IChannelServerConnection connection) && throwExceptionIfMissing) throw new Exception($"Invalid channel id '{authId}'");
            return connection;
        }

        protected bool started = false;

        /// <summary>
        /// Starts the channel server
        /// </summary>
        public void Start() {
            if (this.started) throw new Exception("Channel Server already started");
            //if (this.registeredRouteByPath.Count() == 0) throw new Exception("Must register at least one route");
            this.started = true;
            Task backgroundTask = Task.Run(this.CheckForDeadChannelsAsync);
            this.DoStart();
        }

        protected abstract void DoStart();

        protected async Task CheckForDeadChannelsAsync() {
            while (this.started) {
                DateTime cutoffDateTime = DateTime.Now.AddMilliseconds(-this.mustReceiveHeartbeatMillis);
                DateTime? oldestLastReceivedHeartbeatReceived = null;

                // Check for dead unauthenticated channels
                foreach (IChannelServerConnection channel in this.unauthenticatedConnections.Values.ToArray()) {
                    try {
                        logger.Trace($"CheckForDeadChannelsAsync():channel.Created={channel.Created},cutoffDateTime={cutoffDateTime}");
                        if (channel.Created < cutoffDateTime) {
                            bool removed = this.unauthenticatedConnections.TryRemove(channel, out IChannelServerConnection removedChannel);
                            if (!removed) logger.Error($"Could not remove unauthenticated channel {channel.Id}");
                            channel.Dispose();
                        }
                        else if (oldestLastReceivedHeartbeatReceived == null || oldestLastReceivedHeartbeatReceived > channel.Created) {
                            oldestLastReceivedHeartbeatReceived = channel.Created;
                        }
                    }
                    catch (Exception e) {
                        logger.Error(e);
                    }
                }

                // Check for dead authenticated channels
                foreach ((string id, IChannelServerConnection channel) in this.authenticatedConnectionByAuthId.ToArray()) {
                    try {
                        logger.Trace($"CheckForDeadChannelsAsync():channel.LastHeartbeatReceived={channel.LastHeartbeat},cutoffDateTime={cutoffDateTime}");
                        if (channel.LastHeartbeat < cutoffDateTime) {
                            bool removed = this.authenticatedConnectionByAuthId.TryRemove(id, out IChannelServerConnection removedChannel);
                            if (!removed) logger.Error($"Could not remove authenticated channel {channel.Id}");
                            channel.Dispose();
                        }
                        else if (oldestLastReceivedHeartbeatReceived == null || oldestLastReceivedHeartbeatReceived > channel.LastHeartbeat) {
                            oldestLastReceivedHeartbeatReceived = channel.LastHeartbeat;
                        }
                    }
                    catch (Exception e) {
                        logger.Error(e);
                    }
                }

                int delayMillis = oldestLastReceivedHeartbeatReceived == null ? this.mustReceiveHeartbeatMillis : (int)(oldestLastReceivedHeartbeatReceived.Value.AddMilliseconds(this.mustReceiveHeartbeatMillis) - DateTime.Now).TotalMilliseconds;
                logger.Trace($"CheckForDeadChannelsAsync():delayMillis={delayMillis}");
                await Task.Delay(delayMillis);
            }
            logger.Warn("CheckForDeadChannelsAsync() exiting");
        }

        public void Dispose() {
            this.started = false;

            foreach (var connection in this.unauthenticatedConnections.Values) {
                connection.Dispose();
            }
            this.unauthenticatedConnections.Clear();

            foreach (var connection in this.authenticatedConnectionByAuthId.Values) {
                connection.Dispose();
            }
            this.authenticatedConnectionByAuthId.Clear();
        }
    }

}
