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
using System.Net.Http.Headers;

namespace Butterfly.Channel {
    /// <summary>
    /// Base class implementing <see cref="IChannelServer"/>. New implementations will normally extend this class.
    /// </summary>
    public abstract class BaseChannelServer : IChannelServer {
        protected static readonly Logger logger = LogManager.GetCurrentClassLogger();

        protected readonly ConcurrentDictionary<IChannel, IChannel> unauthenticatedChannels = new ConcurrentDictionary<IChannel, IChannel>();
        protected readonly ConcurrentDictionary<string, IChannel> authenticatedChannelById = new ConcurrentDictionary<string, IChannel>();

        protected readonly List<NewChannelListener> onNewChannelListeners = new List<NewChannelListener>();

        protected readonly Func<string, string> authenticate;
        protected readonly int mustReceiveHeartbeatMillis;

        public BaseChannelServer(Func<string, string> authenticate = null, int mustReceiveHeartbeatMillis = 5000) {
            this.authenticate = authenticate;
            this.mustReceiveHeartbeatMillis = mustReceiveHeartbeatMillis;
        }

        public void AddUnauthenticatedChannel(IChannel channel) {
            this.unauthenticatedChannels[channel] = channel;
        }

        public static string StandardAuthenticate(string value) {
            if (!AuthenticationHeaderValue.TryParse(value, out AuthenticationHeaderValue parsedValue)) throw new Exception("Could not parse authentication value");
            return parsedValue.Parameter;
        }

        internal void Authenticate(string auth, BaseChannel channel) {
            logger.Debug($"Authenticate()");

            var authId = this.authenticate==null ? StandardAuthenticate(auth) : this.authenticate(auth);
            if (string.IsNullOrEmpty(authId)) throw new Exception("Authentication failed");

            channel.SetAuthId(authId);

            this.unauthenticatedChannels.TryRemove(channel, out IChannel dummyChannel);

            var existingChannel = this.GetChannel(authId);
            if (existingChannel != null) {
                existingChannel.Dispose();
            }

            var initChannelListeners = this.onNewChannelListeners.Where(x => x.path == channel.Path).ToArray();
            if (initChannelListeners == null || initChannelListeners.Length == 0) throw new Exception($"Invalid path '{channel.Path}'");
            channel.Start(initChannelListeners);
            this.authenticatedChannelById[authId] = channel;
        }

        public IDisposable OnNewChannel(string pathFilter, Func<IChannel, IDisposable> listener) {
            if (this.started) throw new Exception("Cannot call OnNewChannel() after Start()");
            return new ListItemDisposable<NewChannelListener>(onNewChannelListeners, new NewChannelListener(pathFilter, listener));
        }

        public IDisposable OnNewChannelAsync(string pathFilter, Func<IChannel, Task<IDisposable>> listener) {
            if (this.started) throw new Exception("Cannot call OnNewChannelAsync() after Start()");
            return new ListItemDisposable<NewChannelListener>(onNewChannelListeners, new NewChannelListener(pathFilter, listener));
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
            this.started = true;
            Task backgroundTask = Task.Run(this.CheckForDeadChannelsAsync);

            this.DoStart();
        }

        protected abstract void DoStart();

        protected async Task CheckForDeadChannelsAsync() {
            while (this.started) {
                DateTime cutoffDateTime = DateTime.Now.AddMilliseconds(-this.mustReceiveHeartbeatMillis);
                DateTime? oldestLastReceivedHearbeatReceived = null;

                // Check for dead unauthenticated channels
                foreach (IChannel channel in this.unauthenticatedChannels.Values.ToArray()) {
                    logger.Debug($"CheckForDeadChannelsAsync():channel.Created={channel.Created},cutoffDateTime={cutoffDateTime}");
                    if (channel.Created < cutoffDateTime) {
                        bool removed = this.unauthenticatedChannels.TryRemove(channel, out IChannel removedChannel);
                        if (!removed) throw new Exception($"Could not remove unauthenticated channel");
                        channel.Dispose();
                    }
                    else if (oldestLastReceivedHearbeatReceived == null || oldestLastReceivedHearbeatReceived > channel.Created) {
                        oldestLastReceivedHearbeatReceived = channel.Created;
                    }
                }

                // Check for dead authenticated channels
                foreach (IChannel channel in this.authenticatedChannelById.Values.ToArray()) {
                    logger.Debug($"CheckForDeadChannelsAsync():channel.LastHeartbeatReceived={channel.LastHeartbeat},cutoffDateTime={cutoffDateTime}");
                    if (channel.LastHeartbeat < cutoffDateTime) {
                        bool removed = this.authenticatedChannelById.TryRemove(channel.AuthId, out IChannel removedChannel);
                        if (!removed) throw new Exception($"Could not remove channel id {channel.AuthId}");
                        channel.Dispose();
                    }
                    else if (oldestLastReceivedHearbeatReceived==null || oldestLastReceivedHearbeatReceived>channel.LastHeartbeat) {
                        oldestLastReceivedHearbeatReceived = channel.LastHeartbeat;
                    }
                }

                int delayMillis = oldestLastReceivedHearbeatReceived == null ? this.mustReceiveHeartbeatMillis : (int)(oldestLastReceivedHearbeatReceived.Value.AddMilliseconds(this.mustReceiveHeartbeatMillis) - DateTime.Now).TotalMilliseconds;
                logger.Debug($"CheckForDeadChannelsAsync():delayMillis={delayMillis}");
                await Task.Delay(delayMillis);
            }
        }

        public void Dispose() {
            this.started = false;
        }
    }

}
