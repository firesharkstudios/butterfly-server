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
    public abstract class ChannelServer {
        protected static readonly Logger logger = LogManager.GetCurrentClassLogger();

        protected readonly ConcurrentDictionary<string, IChannel> channelById = new ConcurrentDictionary<string, IChannel>();

        protected readonly List<ChannelListener> onNewChannelListeners = new List<ChannelListener>();
        protected readonly List<ChannelListenerAsync> onNewChannelAsyncListeners = new List<ChannelListenerAsync>();

        protected readonly int mustReceiveHeartbeatMillis;

        public ChannelServer(int mustReceiveHeartbeatMillis = 5000) {
            this.mustReceiveHeartbeatMillis = mustReceiveHeartbeatMillis;
        }

        public IDisposable OnNewChannel(string path, Func<IChannel, IDisposable> listener) {
            return new ListItemDisposable<ChannelListener>(onNewChannelListeners, new ChannelListener(path, listener));
        }

        public IDisposable OnNewChannelAsync(string path, Func<IChannel, Task<IDisposable>> listener) {
            return new ListItemDisposable<ChannelListenerAsync>(onNewChannelAsyncListeners, new ChannelListenerAsync(path, listener));
        }

        /// <summary>
        /// Queues a value to be sent to the specified channel
        /// </summary>
        public void Queue(string channelId, object value) {
            if (!this.channelById.TryGetValue(channelId, out IChannel channel)) throw new Exception($"Invalid channel id '{channelId}'");
            channel.Queue(value);
        }

        /// <summary>
        /// Implementing classes should call this when the client creates a new channel to the server
        /// </summary>
        protected void CreateChannel(string id, string path, Func<IChannel> channelFactory) {
            if (this.channelById.TryGetValue(id, out IChannel existingChannel)) {
                existingChannel.Dispose();
            }
            IChannel channel = channelFactory();
            var initChannelListeners = this.onNewChannelListeners.Where(x => x.path == path).Select(x => x.listener).ToArray();
            var initChannelAsyncListeners = this.onNewChannelAsyncListeners.Where(x => x.path == path).Select(x => x.listener).ToArray();
            channel.Start(initChannelListeners, initChannelAsyncListeners);
            this.channelById[id] = channel;
        }

        protected bool started = false;
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
                foreach ((string id, IChannel channel) in this.channelById.ToArray()) {
                    if (channel.LastHeartbeatReceived < cutoffDateTime) {
                        this.channelById.TryRemove(id, out IChannel removedChannel);
                        channel.Dispose();
                    }
                    else if (oldestLastReceivedHearbeatReceived==null || oldestLastReceivedHearbeatReceived>channel.LastHeartbeatReceived) {
                        oldestLastReceivedHearbeatReceived = channel.LastHeartbeatReceived;
                    }
                }
                int delayMillis = oldestLastReceivedHearbeatReceived == null ? this.mustReceiveHeartbeatMillis : (int)(oldestLastReceivedHearbeatReceived.Value.AddMilliseconds(this.mustReceiveHeartbeatMillis) - DateTime.Now).TotalMilliseconds;
                await Task.Delay(delayMillis);
            }
        }

        public void Stop() {
            this.started = false;
        }
    }

    public class ChannelListener {
        public readonly string path;
        public readonly Func<IChannel, IDisposable> listener;

        public ChannelListener(string path, Func<IChannel, IDisposable> listener) {
            this.path = path;
            this.listener = listener;
        }
    }

    public class ChannelListenerAsync {
        public readonly string path;
        public readonly Func<IChannel, Task<IDisposable>> listener;

        public ChannelListenerAsync(string path, Func<IChannel, Task<IDisposable>> listener) {
            this.path = path;
            this.listener = listener;
        }
    }
}
