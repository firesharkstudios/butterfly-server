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

using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

using Unosquare.Labs.EmbedIO;
using Unosquare.Labs.EmbedIO.Constants;

using Butterfly.Util;

namespace Butterfly.WebApi.EmbedIO {
    public class EmbedIOWebApiServer : BaseWebApiServer {

        public readonly Unosquare.Labs.EmbedIO.WebServer server;

        public EmbedIOWebApiServer(Unosquare.Labs.EmbedIO.WebServer server) {
            this.server = server;
        }

        public override void Start() {
            this.server.RegisterModule(new MyWebModule(this.webHandlers));
        }

        public override void Dispose() {
        }
    }

    public class MyWebModule : WebModuleBase {
        public MyWebModule(ICollection<WebHandler> webHandlers) {
            foreach (var webHandler in webHandlers) {
                HttpVerbs httpVerb;
                if (webHandler.method == HttpMethod.Get) {
                    httpVerb = HttpVerbs.Get;
                }
                else if (webHandler.method == HttpMethod.Post) {
                    httpVerb = HttpVerbs.Post;
                }
                else {
                    throw new System.Exception($"Unknown method '{webHandler.method}'");
                }
                this.AddHandler(webHandler.path, httpVerb, async (context, cancellationToken) => {
                    await webHandler.listener(new EmbedIOWebRequest(context), new EmbedIOWebResponse(context));
                    return true;
                });
            }
        }

        public override string Name => "My Web Module";
    }

    public class EmbedIOWebRequest : IWebRequest {

        public readonly Unosquare.Net.HttpListenerContext context;

        public EmbedIOWebRequest(Unosquare.Net.HttpListenerContext context) {
            this.context = context;
        }

        public async Task<T> ParseAsJsonAsync<T>() {
            using (StreamReader reader = new StreamReader(this.context.Request.InputStream)) {
                string json = await reader.ReadToEndAsync();
                return JsonUtil.Deserialize<T>(json);
            }
        }

        public NameValueCollection Headers {
            get {
                return this.context.Request.Headers;
            }
        }

        public AuthenticationHeaderValue AuthenticationHeaderValue {
            get {
                string text = this.Headers[HttpRequestHeader.Authorization.ToString()];
                return AuthenticationHeaderValue.Parse(text);
            }
        }
    }

    public class EmbedIOWebResponse : IWebResponse {

        public readonly Unosquare.Net.HttpListenerContext context;

        public EmbedIOWebResponse(Unosquare.Net.HttpListenerContext context) {
            this.context = context;
        }

        public async Task WriteAsJsonAsync(object value) {
            using (StreamWriter streamWriter = new StreamWriter(this.context.Response.OutputStream)) {
                var json = JsonUtil.Serialize(value);
                await streamWriter.WriteAsync(json);
            }
        }

    }
}
