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

        public readonly Func<string, string, object> getAuthToken;
        public readonly Func<string, string, Task<object>> getAuthTokenAsync;

        public readonly Func<object, string> getId;
        public readonly Func<object, Task<string>> getIdAsync;

        protected readonly Dictionary<string, RegisteredChannel> registeredChannelByKey = new Dictionary<string, RegisteredChannel>();

        public RegisteredRoute(string path, Func<string, string, object> getAuthToken, Func<string, string, Task<object>> getAuthTokenAsync, Func<object, string> getId = null, Func<object, Task<string>> getIdAsync = null) {
            this.path = path;

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
                this.getId = authToken => authToken.ToString();
                this.getIdAsync = null;
            }
        }

        public Dictionary<string, RegisteredChannel> RegisteredChannelByKey => this.registeredChannelByKey;

        public RegisteredChannel RegisterChannel(string channelKey = "default", Func<Dict, Channel, IDisposable> handler = null, Func<Dict, Channel, Task<IDisposable>> handlerAsync = null) {
            if (this.registeredChannelByKey.TryGetValue(channelKey, out RegisteredChannel registerChannel)) throw new Exception($"Already a registered channel '{channelKey}'");
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
