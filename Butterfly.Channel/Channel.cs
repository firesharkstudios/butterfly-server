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
    public class Channel {

        public readonly string id;

        protected readonly BaseChannelServer channelManager;

        protected readonly ConcurrentQueue<string> buffer = new ConcurrentQueue<string>();
        protected readonly AsyncMonitor monitor = new AsyncMonitor();

        public Channel(BaseChannelServer channelManager, string id) {
            this.channelManager = channelManager;
            this.id = id;
        }

        public string Id => this.id;

        public void Queue(object value, string path = null) {
            string json = JsonUtil.Serialize(new {
                path,
                value
            });
            this.buffer.Enqueue(json);
            this.monitor.PulseAll();
        }

        protected bool started = false;
        public void Start(Func<Channel, IDisposable> initChannel) {
            this.started = true;
            Task.Run(() => this.Run(initChannel));
        }

        public void Start(Func<Channel, Task<IDisposable>> initChannelAsync) {
            this.started = true;
            Task.Run(() => this.Run(initChannelAsync));
        }

        public void Stop() {
            this.started = false;
            this.monitor.PulseAll();
        }

        protected async Task Run(Func<Channel, IDisposable> initChannel) {
            using (var attachment = initChannel(this)) {
                await this.DoRun();
            }
        }

        protected async Task Run(Func<Channel, Task<IDisposable>> initChannelAsync) {
            using (var attachment = await initChannelAsync(this)) {
                await this.DoRun();
            }
        }

        protected async Task DoRun() {
            IChannelTransport channelTransport = this.channelManager.GetChannelTransport(this.Id);
            while (this.started) {
                if (this.buffer.TryDequeue(out string result)) {
                    await channelTransport.Send(result);
                }
                else {
                    using (await this.monitor.EnterAsync()) {
                        await this.monitor.WaitAsync();
                    }
                }
            }
        }
    }
}
