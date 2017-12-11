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
using System.Net;
using System.Threading.Tasks;

using Nito.AsyncEx;
using NLog;

using Butterfly.Util;

namespace Butterfly.Channel {

    /// <inheritdoc/>
    /// <summary>
    /// Base class implementing <see cref="IChannel"/>. New implementations will normally extend this class.
    /// </summary>
    public abstract class BaseChannel : IChannel {
        protected static readonly Logger logger = LogManager.GetCurrentClassLogger();

        protected readonly BaseChannelServer channelServer;
        protected readonly string path;
        protected readonly DateTime created;

        protected readonly ConcurrentQueue<string> buffer = new ConcurrentQueue<string>();
        protected readonly AsyncMonitor monitor = new AsyncMonitor();

        /// <summary>
        /// Stores when the datetime of the last heartbeat (set via <ref>Heartbeat</ref>)
        /// </summary>
        protected DateTime lastHeartbeat = DateTime.Now;

        protected string authId = null;

        public BaseChannel(BaseChannelServer channelServer, string path) {
            this.channelServer = channelServer;
            this.path = path;
            this.created = DateTime.Now;
        }

        public string Path => this.path;

        /// <summary>
        /// Unique identifier for the channel
        /// </summary>
        public string AuthId => this.authId;

        internal void SetAuthId(string value) {
            this.authId = value;
        }

        public DateTime Created => this.created;

        /// <summary>
        /// When the last heartbeat was registered
        /// </summary>
        public DateTime LastHeartbeat => this.lastHeartbeat;

        /// <summary>
        /// Implementing classes should call this periodically to keep the channel alive (otherwise <ref>ChannelServer</ref> will remove the channel)
        /// </summary>
        public void Heartbeat() {
            logger.Trace($"Heartbeat()");
            this.lastHeartbeat = DateTime.Now;
        }

        /// <summary>
        /// Queue an object to be sent over the channel to the client.  The queue is processed by a background thread when the Channel is started.
        /// </summary>
        /// <param name="value">The value to be sent to the client (will be converted to JSON)</param>
        public void Queue(object value) {
            string json = JsonUtil.Serialize(value);
            this.buffer.Enqueue(json);
            this.monitor.PulseAll();
        }

        protected bool started = false;
        public void Start(ICollection<NewChannelListener> newChannelListeners) {
            this.started = true;
            Task.Run(() => this.RunAsync(newChannelListeners));
        }

        protected async Task RunAsync(ICollection<NewChannelListener> initChannelListeners) {
            List<IDisposable> disposables = new List<IDisposable>();
            try {
                foreach (var listener in initChannelListeners) {
                    var disposable = listener.listener!=null ? listener.listener(this) : await listener.listenerAsync(this);
                    if (disposable != null) disposables.Add(disposable);
                }
                while (this.started) {
                    if (this.buffer.TryDequeue(out string result)) {
                        await this.SendAsync(result);
                    }
                    else {
                        using (await this.monitor.EnterAsync()) {
                            await this.monitor.WaitAsync();
                        }
                    }
                }
            }
            finally {
                foreach (var disposable in disposables) {
                    disposable.Dispose();
                }
            }
        }

        /// <summary>
        /// Implementing classes must override this to actually send the text to the client
        /// </summary>
        protected abstract Task SendAsync(string text);

        public void ReceiveMessage(string text) {
            if (text == "!") {
                this.Heartbeat();
            }
            else {
                int pos = text.IndexOf(':');
                if (pos > 0) {
                    string header = text.Substring(0, pos).Trim();
                    string value = text.Substring(pos + 1).Trim();
                    if (header == HttpRequestHeader.Authorization.ToString()) {
                        this.channelServer.Authenticate(value, this);
                    }
                }
            }
        }

        /// <summary>
        /// Implements the IDispose interface
        /// </summary>
        public void Dispose() {
            this.started = false;
            this.monitor.PulseAll();
            this.DoDispose();
        }

        /// <summary>
        /// Implementing classes may optionally override this to cleanup resources as appropriate
        /// </summary>
        protected virtual void DoDispose() {
        }

    }
}
