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

using Butterfly.Util;

namespace Butterfly.Channel {

    /// <inheritdoc/>
    /// <summary>
    /// Base class implementing <see cref="IChannelServer"/>. New implementations will normally extend this class.
    /// </summary>
    public abstract class BaseChannelServer : IChannelServer {
        protected static readonly Logger logger = LogManager.GetCurrentClassLogger();

        protected readonly ConcurrentDictionary<IChannel, IChannel> unauthenticatedChannels = new ConcurrentDictionary<IChannel, IChannel>();
        protected readonly ConcurrentDictionary<string, IChannel> authenticatedChannelById = new ConcurrentDictionary<string, IChannel>();

        protected readonly List<NewChannelHandler> onNewChannelHandlers = new List<NewChannelHandler>();

        protected readonly int mustReceiveHeartbeatMillis;

        public BaseChannelServer(int mustReceiveHeartbeatMillis = 5000) {
            this.mustReceiveHeartbeatMillis = mustReceiveHeartbeatMillis;
        }

        public void AddUnauthenticatedChannel(IChannel channel) {
            //logger.Debug($"AddUnauthenticatedChannel():channel.Path={channel.WebRequest.RequestUri.AbsolutePath}");
            this.unauthenticatedChannels[channel] = channel;
        }

        /*
        public static T StandardAuthenticate(string value) {
            if (!AuthenticationHeaderValue.TryParse(value, out AuthenticationHeaderValue parsedValue)) throw new Exception("Could not parse authentication value");
            return parsedValue.Parameter;
        }
        */

        internal void Authenticate(string authType, string authValue, BaseChannel channel) {
            this.unauthenticatedChannels.TryRemove(channel, out IChannel dummyChannel);
            Task task = this.AuthenticateAsync(authType, authValue, channel);
        }

        internal async Task AuthenticateAsync(string authType, string authValue, BaseChannel channel) {
            var onNewChannelHandler = this.onNewChannelHandlers.Where(x => x.path == channel.WebRequest.RequestUri.AbsolutePath).FirstOrDefault();
            if (onNewChannelHandler == null) throw new Exception($"Invalid path '{channel.WebRequest.RequestUri.AbsolutePath}'");
            try {
                string id = onNewChannelHandler.handle != null ? onNewChannelHandler.handle(authType, authValue, channel) : await onNewChannelHandler.handleAsync(authType, authValue, channel);
                if (!string.IsNullOrEmpty(id)) {
                    var existingChannel = this.GetChannel(id);
                    if (existingChannel != null) {
                        existingChannel.Dispose();
                    }

                    channel.Start();
                    this.authenticatedChannelById[id] = channel;
                }
            }
            catch (Exception e) {
                logger.Error(e);
            }
        }

        /// <inheritdoc/>
        public IDisposable OnNewChannel(string path, Func<string, string, IChannel, string> handler) {
            if (this.started) throw new Exception("Cannot call OnNewChannel() after Start()");
            return new ListItemDisposable<NewChannelHandler>(onNewChannelHandlers, new NewChannelHandler(path, handler));
        }

        /// <inheritdoc/>
        public IDisposable OnNewChannel(string path, Func<string, string, IChannel, Task<string>> handler) {
            if (this.started) throw new Exception("Cannot call OnNewChannelAsync() after Start()");
            return new ListItemDisposable<NewChannelHandler>(onNewChannelHandlers, new NewChannelHandler(path, handler));
        }

        public int ChannelCount => this.authenticatedChannelById.Count;

        public IChannel GetChannel(string channelId, bool throwExceptionIfMissing = false) {
            if (!this.authenticatedChannelById.TryGetValue(channelId, out IChannel channel) && throwExceptionIfMissing) throw new Exception($"Invalid channel id '{channelId}'");
            return channel;
        }

        public void Queue(string channelId, object value) {
            if (!this.started) throw new Exception("Cannot call Queue() before Start()");
            var channel = this.GetChannel(channelId);
            channel.Queue(value);
        }

        protected bool started = false;

        /// <summary>
        /// Starts the channel server
        /// </summary>
        public void Start() {
            if (this.started) throw new Exception("Channel Server already started");

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
                foreach (IChannel channel in this.unauthenticatedChannels.Values.ToArray()) {
                    logger.Trace($"CheckForDeadChannelsAsync():channel.Created={channel.Created},cutoffDateTime={cutoffDateTime}");
                    if (channel.Created < cutoffDateTime) {
                        bool removed = this.unauthenticatedChannels.TryRemove(channel, out IChannel removedChannel);
                        if (!removed) throw new Exception($"Could not remove unauthenticated channel");
                        channel.Dispose();
                    }
                    else if (oldestLastReceivedHeartbeatReceived == null || oldestLastReceivedHeartbeatReceived > channel.Created) {
                        oldestLastReceivedHeartbeatReceived = channel.Created;
                    }
                }

                // Check for dead authenticated channels
                foreach ((string id, IChannel channel) in this.authenticatedChannelById.ToArray()) {
                    logger.Trace($"CheckForDeadChannelsAsync():channel.LastHeartbeatReceived={channel.LastHeartbeat},cutoffDateTime={cutoffDateTime}");
                    if (channel.LastHeartbeat < cutoffDateTime) {
                        bool removed = this.authenticatedChannelById.TryRemove(id, out IChannel removedChannel);
                        if (!removed) throw new Exception($"Could not remove channel id {id}");
                        channel.Dispose();
                    }
                    else if (oldestLastReceivedHeartbeatReceived==null || oldestLastReceivedHeartbeatReceived>channel.LastHeartbeat) {
                        oldestLastReceivedHeartbeatReceived = channel.LastHeartbeat;
                    }
                }

                int delayMillis = oldestLastReceivedHeartbeatReceived == null ? this.mustReceiveHeartbeatMillis : (int)(oldestLastReceivedHeartbeatReceived.Value.AddMilliseconds(this.mustReceiveHeartbeatMillis) - DateTime.Now).TotalMilliseconds;
                logger.Trace($"CheckForDeadChannelsAsync():delayMillis={delayMillis}");
                await Task.Delay(delayMillis);
            }
        }

        public void Dispose() {
            this.started = false;
        }
    }

}
