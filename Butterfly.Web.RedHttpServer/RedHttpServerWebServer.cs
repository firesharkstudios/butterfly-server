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

namespace Butterfly.Web.RedHttpServer {
    public class RedHttpServerWebServer : WebServer {

        protected readonly global::RedHttpServerNet45.RedHttpServer server;

        public RedHttpServerWebServer(global::RedHttpServerNet45.RedHttpServer server) {
            this.server = server;
        }

        public override void Start() {
            foreach (var webHandler in this.webHandlers) {
                if (webHandler.method == HttpMethod.Get) {
                    this.server.Get(webHandler.path, async (req, res) => {
                        await webHandler.run(new RedHttpServerWebRequest(req), new RedHttpServerWebResponse(res));
                    });
                }
                else if (webHandler.method == HttpMethod.Post) {
                    this.server.Post(webHandler.path, async (req, res) => {
                        await webHandler.run(new RedHttpServerWebRequest(req), new RedHttpServerWebResponse(res));
                    });
                }
            }
        }

        public override void Dispose() {
        }
    }

    public class RedHttpServerWebRequest : IWebRequest {

        protected readonly RRequest request;

        public RedHttpServerWebRequest(RRequest request) {
            this.request = request;
        }

        public async Task<T> ParseAsJsonAsync<T>() {
            using (StreamReader reader = new StreamReader(this.request.GetBodyStream())) {
                string json = await reader.ReadToEndAsync();
                return JsonUtil.Deserialize<T>(json);
            }
            //return await this.request.ParseBodyAsync<T>();
        }

        public NameValueCollection Headers {
            get {
                return this.request.Headers;
            }
        }

        public AuthenticationHeaderValue AuthenticationHeaderValue {
            get {
                string text = this.request.Headers[HttpRequestHeader.Authorization.ToString()];
                return AuthenticationHeaderValue.Parse(text);
            }
        }
    }

    public class RedHttpServerWebResponse : IWebResponse {

        protected readonly RResponse response;

        public RedHttpServerWebResponse(RResponse response) {
            this.response = response;
        }

        public void WriteAsJson(object value) {
            this.response.SendJson(value);
        }

    }
}
