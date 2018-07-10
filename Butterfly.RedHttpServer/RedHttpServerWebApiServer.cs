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
using System.Net.Http;
using System.Threading.Tasks;

using RedHttpServerNet45.Request;
using RedHttpServerNet45.Response;

using Butterfly.Core.Util;
using Butterfly.Core.WebApi;

namespace Butterfly.RedHttpServer {

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
                        try {
                            await webHandler.listener(new RedHttpServerWebRequest(req), new RedHttpServerWebResponse(res));
                        }
                        catch (Exception e) {
                            logger.Error(e);
                        }
                    });
                }
                else if (webHandler.method == HttpMethod.Post) {
                    this.server.Post(webHandler.path, async (req, res) => {
                        try {
                            await webHandler.listener(new RedHttpServerWebRequest(req), new RedHttpServerWebResponse(res));
                        }
                        catch (Exception e) {
                            logger.Error(e);
                        }
                    });
                }
            }
        }

        public override void Dispose() {
        }
    }

    public class RedHttpServerWebRequest : BaseHttpRequest {

        public readonly RRequest request;

        public RedHttpServerWebRequest(RRequest request) {
            this.request = request;
        }

        protected override Stream InputStream => this.request.GetBodyStream();

        public override Uri RequestUrl => this.request.UnderlyingRequest.Url;

        public override string UserAgent => this.request.UnderlyingRequest.UserAgent;

        public override string UserHostAddress => this.request.UnderlyingRequest.UserHostAddress;

        public override string UserHostName => this.request.UnderlyingRequest.UserHostName;

        public override Dictionary<string, string> Headers => this.request.Headers.ToDictionary();

        public override Dictionary<string, string> PathParams => throw new NotImplementedException();

        public override Dictionary<string, string> QueryParams => this.RequestUrl.ParseQuery();
    }

    public class RedHttpServerWebResponse : IHttpResponse {

        public readonly RResponse response;

        public RedHttpServerWebResponse(RResponse response) {
            this.response = response;
        }

        public string GetHeader(string name) {
            throw new NotImplementedException();
        }

        public void SetHeader(string name, string value) {
            throw new NotImplementedException();
        }

        public int StatusCode {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public string StatusText {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public void SendRedirect(string url) {
            throw new NotImplementedException();
        }

        public Stream OutputStream => this.response.UnderlyingResponse.OutputStream;

        public Task WriteAsTextAsync(string value) {
            this.response.SendString(value);
            return Task.FromResult(0);
        }

        public Task WriteAsJsonAsync(object value) {
            this.response.SendJson(value);
            return Task.FromResult(0);
        }

    }
}
