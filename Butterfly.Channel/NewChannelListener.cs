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
using System.Threading.Tasks;

namespace Butterfly.Channel {
    /// <summary>
    /// Internal class used to store references to new channel listeners
    /// </summary>
    public class NewChannelHandler {
        public readonly string path;
        public readonly Func<string, string, IChannel, string> handle;
        public readonly Func<string, string, IChannel, Task<string>> handleAsync;

        public NewChannelHandler(string path, Func<string, string, IChannel, string> listener) {
            this.path = path;
            this.handle = listener;
            this.handleAsync = null;
        }

        public NewChannelHandler(string pathFilter, Func<string, string, IChannel, Task<string>> listener) {
            this.path = pathFilter;
            this.handle = null;
            this.handleAsync = listener;
        }
    }
}
