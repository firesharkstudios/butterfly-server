/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using Unosquare.Labs.EmbedIO;
using Unosquare.Labs.EmbedIO.Constants;

using NLog;

using Butterfly.Core.Util;
using Butterfly.Core.WebApi;

namespace Butterfly.EmbedIO {

    /// <inheritdoc/>
    public class EmbedIOWebApi : BaseWebApi {

        public readonly Unosquare.Labs.EmbedIO.WebServer server;

        public EmbedIOWebApi(Unosquare.Labs.EmbedIO.WebServer server) {
            this.server = server;
        }

        public override void Compile() {
            this.server.RegisterModule(new MyWebModule(this.webHandlers));
        }

        public override void Dispose() {
        }
    }

    public class MyWebModule : WebModuleBase {
        protected static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public MyWebModule(ICollection<Core.WebApi.WebHandler> webHandlers) {
            foreach (var webHandler in webHandlers) {
                HttpVerbs httpVerb;
                if (webHandler.method == HttpMethod.Delete) {
                    httpVerb = HttpVerbs.Delete;
                }
                else if (webHandler.method == HttpMethod.Get) {
                    httpVerb = HttpVerbs.Get;
                }
                else if (webHandler.method == HttpMethod.Post) {
                    httpVerb = HttpVerbs.Post;
                }
                else if (webHandler.method == HttpMethod.Put) {
                    httpVerb = HttpVerbs.Put;
                }
                else {
                    throw new System.Exception($"Unknown method '{webHandler.method}'");
                }
                this.AddHandler(webHandler.path, httpVerb, async (context, cancellationToken) => {
                    var webRequest = new EmbedIOHttpWebRequest(webHandler.path, context);
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

    public class EmbedIOHttpWebRequest : BaseHttpRequest {

        public readonly string pathPattern;
        public readonly IHttpContext context;

        public EmbedIOHttpWebRequest(string pathPattern, IHttpContext context) {
            this.pathPattern = pathPattern;
            this.context = context;
        }

        protected override Stream InputStream => this.context.Request.InputStream;

        public override Uri RequestUrl => this.context.Request.Url;

        public override Dictionary<string, string> Headers => this.context.Request.Headers?.ToDictionary(forceUpperCaseKeys: true);

        public override Dictionary<string, string> PathParams {
            get {
                var regexUrlParams = this.context.RequestRegexUrlParams(this.pathPattern);
                return regexUrlParams.ToDictionary(x => x.Key, x => Convert.ToString(x.Value));
            }
        }

        public override Dictionary<string, string> QueryParams => this.RequestUrl.ParseQuery();

    }

    public class EmbedIOWebResponse : Core.WebApi.IHttpResponse {

        public readonly IHttpContext context;

        public EmbedIOWebResponse(IHttpContext context) {
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

        public async Task WriteAsTextAsync(string value) {
            using (StreamWriter streamWriter = new StreamWriter(this.context.Response.OutputStream)) {
                await streamWriter.WriteAsync(value);
            }
        }

        /*
        public Task WriteAsJsonAsync(object value) {
        }
        */

    }
}
