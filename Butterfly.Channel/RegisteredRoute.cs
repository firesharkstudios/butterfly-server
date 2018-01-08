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

namespace Butterfly.Channel {
    /// <summary>
    /// Internal class used to store references to new channel listeners
    /// </summary>
    public class RegisteredRoute {

        public readonly string path;
        public readonly Func<string, string, string> getAuthId;
        public readonly Func<string, string, Task<string>> getAuthIdAsync;

        protected readonly Dictionary<string, RegisteredChannel> registeredChannelByKey = new Dictionary<string, RegisteredChannel>();

        public RegisteredRoute(string path, Func<string, string, string> getAuthId) {
            this.path = path;
            this.getAuthId = getAuthId;
            this.getAuthIdAsync = null;
        }

        public RegisteredRoute(string path, Func<string, string, Task<string>> getAuthIdAsync) {
            this.path = path;
            this.getAuthId = null;
            this.getAuthIdAsync = getAuthIdAsync;
        }

        public Dictionary<string, RegisteredChannel> RegisteredChannelByKey => this.registeredChannelByKey;

        public RegisteredChannel RegisterChannel(string channelKey = "default", Func<Dict, Channel, IDisposable> handler = null, Func<Dict, Channel, Task<IDisposable>> handlerAsync = null) {
            if (this.registeredChannelByKey.TryGetValue(channelKey, out RegisteredChannel registerChannel)) throw new Exception("Already a registered channel '{channelKey}'");
            if (handler!=null && handlerAsync!=null) {
                throw new Exception("Can only specify a handler or handlerAsync but not both");
            }
            else if (handler != null) {
                registerChannel = new RegisteredChannel(channelKey, handler);
            }
            else if (handlerAsync != null) {
                registerChannel = new RegisteredChannel(channelKey, handlerAsync);
            }
            else {
                throw new Exception("Must specify a handler for new channels");
            }
            this.registeredChannelByKey[channelKey] = registerChannel;
            return registerChannel;
        }

    }
}
