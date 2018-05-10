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
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using Unosquare.Labs.EmbedIO;
using Unosquare.Labs.EmbedIO.Constants;

using Butterfly.Util;
using NLog;

namespace Butterfly.WebApi.EmbedIO {

    /// <inheritdoc/>
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
        protected static readonly Logger logger = LogManager.GetCurrentClassLogger();

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
                    var webRequest = new EmbedIOWebRequest(webHandler.path, context);
                    var webResponse = new EmbedIOWebResponse(context);
                    try {
                        await webHandler.listener(webRequest, webResponse);
                    }
                    catch (IOException) {
                    }
                    catch (Exception e) {
                        if (!(e is UnauthorizedException)) {
                            logger.Error(e);
                        }
                        webResponse.StatusCode = 500;
                        webResponse.StatusText = e.Message;
                        await webResponse.WriteAsJsonAsync(e.Message);
                    }
                    return true;
                });
            }
        }

        public override string Name => "My Web Module";
    }

    public class EmbedIOWebRequest : BaseHttpRequest {

        public readonly string pathPattern;
        public readonly Unosquare.Net.HttpListenerContext context;

        public EmbedIOWebRequest(string pathPattern, Unosquare.Net.HttpListenerContext context) {
            this.pathPattern = pathPattern;
            this.context = context;
        }

        protected override Stream InputStream => this.context.Request.InputStream;

        public override Uri RequestUrl => this.context.Request.Url;

        public override string UserAgent => this.context.Request.UserAgent;

        public override string UserHostAddress => this.context.Request.UserHostAddress;

        public override string UserHostName => this.context.Request.UserHostName;

        public override Dictionary<string, string> Headers => this.context.Request.Headers?.ToDictionary(forceUpperCaseKeys: true);

        public override Dictionary<string, string> PathParams {
            get {
                var regexUrlParams = this.context.RequestRegexUrlParams(this.pathPattern);
                return regexUrlParams.ToDictionary(x => x.Key, x => Convert.ToString(x.Value));
            }
        }

        public override Dictionary<string, string> QueryParams => this.RequestUrl.ParseQuery();

    }

    public class EmbedIOWebResponse : IHttpResponse {

        public readonly Unosquare.Net.HttpListenerContext context;

        public EmbedIOWebResponse(Unosquare.Net.HttpListenerContext context) {
            this.context = context;
        }

        public string GetHeader(string name) {
            return this.context.Response.Headers.Get(name);
        }

        public void SetHeader(string name, string value) {
            if (name == "Content-Type") {
                this.context.Response.ContentType = value;
            }
            else {
                this.context.Response.Headers.Set(name, value);
            }
        }

        public int StatusCode {
            get {
                return this.context.Response.StatusCode;
            }
            set {
                this.context.Response.StatusCode = value;
            }
        }

        public string StatusText {
            get {
                return this.context.Response.StatusDescription;
            }
            set {
                this.context.Response.StatusDescription = value;
            }
        }

        public void SendRedirect(string url) {
            this.context.Redirect(url);
        }

        public Stream OutputStream => this.context.Response.OutputStream;

        public async Task WriteAsJsonAsync(object value) {
            using (StreamWriter streamWriter = new StreamWriter(this.context.Response.OutputStream)) {
                var json = JsonUtil.Serialize(value);
                await streamWriter.WriteAsync(json);
            }
        }

    }
}
