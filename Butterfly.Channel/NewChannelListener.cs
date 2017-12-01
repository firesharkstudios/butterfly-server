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
    public class NewChannelListener {
        public readonly string pathFilter;
        public readonly Func<IChannel, IDisposable> listener;
        public readonly Func<IChannel, Task<IDisposable>> listenerAsync;

        public NewChannelListener(string path, Func<IChannel, IDisposable> listener) {
            this.pathFilter = path;
            this.listener = listener;
            this.listenerAsync = null;
        }

        public NewChannelListener(string pathFilter, Func<IChannel, Task<IDisposable>> listener) {
            this.pathFilter = pathFilter;
            this.listener = null;
            this.listenerAsync = listener;
        }
    }
}
