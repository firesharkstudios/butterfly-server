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
using System.Net.Http;
using System.Threading.Tasks;

namespace Butterfly.WebApi {

    /// <inheritdoc/>
    /// <summary>
    /// Base class implementing <see cref="IWebApiServer"/>. New implementations will normally extend this class.
    /// </summary>
    public abstract class BaseWebApiServer : IWebApiServer {
        protected readonly List<WebHandler> webHandlers = new List<WebHandler>();

        public void OnGet(string path, Func<IWebRequest, IWebResponse, Task> listener) {
            webHandlers.Add(new WebHandler {
                method = HttpMethod.Get,
                path = path,
                listener = listener
            });
        }

        public void OnPost(string path, Func<IWebRequest, IWebResponse, Task> listener) {
            webHandlers.Add(new WebHandler {
                method = HttpMethod.Post,
                path = path,
                listener = listener
            });
        }

        public List<WebHandler> WebHandlers {
            get {
                return this.webHandlers;
            }
        }

        public abstract void Start();
        public abstract void Dispose();
    }
}
