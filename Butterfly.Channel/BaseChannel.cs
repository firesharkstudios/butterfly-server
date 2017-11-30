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
using System.Threading.Tasks;

using Nito.AsyncEx;

using Butterfly.Util;

namespace Butterfly.Channel {
    public abstract class BaseChannel : IChannel {
        protected readonly string id;

        protected readonly ConcurrentQueue<string> buffer = new ConcurrentQueue<string>();
        protected readonly AsyncMonitor monitor = new AsyncMonitor();

        protected DateTime lastHeartbeatReceived = DateTime.Now;

        public BaseChannel(string id) {
            this.id = id;
        }

        public string Id => this.id;

        /// <summary>
        /// Implementing classes must keep this updated to avoid the channel being killed by the <ref>ChannelServer</ref>
        /// </summary>
        public DateTime LastHeartbeatReceived => this.lastHeartbeatReceived;

        public void Heartbeat() {
            this.lastHeartbeatReceived = DateTime.Now;
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
        public void Start(ICollection<ChannelListener> initChannelListeners) {
            this.started = true;
            Task.Run(() => this.RunAsync(initChannelListeners));
        }

        protected async Task RunAsync(ICollection<ChannelListener> initChannelListeners) {
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
