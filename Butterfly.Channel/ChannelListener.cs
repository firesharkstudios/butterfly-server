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
    public class ChannelListener {
        public readonly string path;
        public readonly Func<IChannel, IDisposable> listener;
        public readonly Func<IChannel, Task<IDisposable>> listenerAsync;

        public ChannelListener(string path, Func<IChannel, IDisposable> listener) {
            this.path = path;
            this.listener = listener;
            this.listenerAsync = null;
        }

        public ChannelListener(string path, Func<IChannel, Task<IDisposable>> listener) {
            this.path = path;
            this.listener = null;
            this.listenerAsync = listener;
        }
    }
}
