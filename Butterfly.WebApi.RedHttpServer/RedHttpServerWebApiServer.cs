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

using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

using RedHttpServerNet45.Request;
using RedHttpServerNet45.Response;

using Butterfly.Util;

namespace Butterfly.WebApi.RedHttpServer {

    /// <inheritdoc/>
    public class RedHttpServerWebApiServer : BaseWebApiServer {

        public readonly global::RedHttpServerNet45.RedHttpServer server;

        public RedHttpServerWebApiServer(global::RedHttpServerNet45.RedHttpServer server) {
            this.server = server;
        }

        public override void Start() {
            foreach (var webHandler in this.webHandlers) {
                if (webHandler.method == HttpMethod.Get) {
                    this.server.Get(webHandler.path, async (req, res) => {
                        await webHandler.listener(new RedHttpServerWebRequest(req), new RedHttpServerWebResponse(res));
                    });
                }
                else if (webHandler.method == HttpMethod.Post) {
                    this.server.Post(webHandler.path, async (req, res) => {
                        await webHandler.listener(new RedHttpServerWebRequest(req), new RedHttpServerWebResponse(res));
                    });
                }
            }
        }

        public override void Dispose() {
        }
    }

    public class RedHttpServerWebRequest : IWebRequest {

        public readonly RRequest request;

        public RedHttpServerWebRequest(RRequest request) {
            this.request = request;
        }

        public async Task<T> ParseAsJsonAsync<T>() {
            using (StreamReader reader = new StreamReader(this.request.GetBodyStream())) {
                string json = await reader.ReadToEndAsync();
                return JsonUtil.Deserialize<T>(json);
            }
        }

        public NameValueCollection Headers {
            get {
                return this.request.Headers;
            }
        }

        public AuthenticationHeaderValue AuthenticationHeaderValue {
            get {
                string text = this.Headers[HttpRequestHeader.Authorization.ToString()];
                return AuthenticationHeaderValue.Parse(text);
            }
        }
    }

    public class RedHttpServerWebResponse : IWebResponse {

        public readonly RResponse response;

        public RedHttpServerWebResponse(RResponse response) {
            this.response = response;
        }

        public Task WriteAsJsonAsync(object value) {
            this.response.SendJson(value);
            return Task.FromResult(0);
        }

    }
}
