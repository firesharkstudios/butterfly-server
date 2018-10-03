/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;

using NLog;
using Unosquare.Labs.EmbedIO.Modules;

using Butterfly.Core.Channel;
using Butterfly.Core.WebApi;
using Butterfly.Core.Util;

namespace Butterfly.EmbedIO {
    /// <summary>
    /// Convenient class to initialize IWebApi and ISubscriptionApi instances using EmbedIO (see https://github.com/unosquare/embedio)
    /// </summary>
    public class EmbedIOContext : IDisposable {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        protected readonly Unosquare.Labs.EmbedIO.WebServer webServer;
        protected readonly IWebApi webApi;
        protected readonly ISubscriptionApi subscriptionApi;

        public EmbedIOContext(string url, string staticPath = null) {
            // Binding to all local IP addresses requires adding an HTTP URL ACL rule
            // This may prompt to "allow app to modify your device"
            ProcessX.AddHttpUrlAclIfNeeded(url);

            // Create the underlying EmbedIOWebServer (see https://github.com/unosquare/embedio)
            this.webServer = new Unosquare.Labs.EmbedIO.WebServer(url);
            if (!string.IsNullOrEmpty(staticPath)) {
                logger.Debug($"EmbedIOContext():staticPath={staticPath}");
                this.webServer.RegisterModule(new StaticFilesModule(staticPath, headers: new System.Collections.Generic.Dictionary<string, string> {
                    ["Cache-Control"] = "no-cache, no-store, must-revalidate",
                    ["Pragma"] = "no-cache",
                    ["Expires"] = "0"
                }));
            }
            //Unosquare.Swan.Terminal.Settings.DisplayLoggingMessageType = Unosquare.Swan.LogMessageType.Trace;

            // Create the IWebApi and ISubscriptionApi wrappers
            this.webApi = new EmbedIOWebApi(webServer);
            this.subscriptionApi = new EmbedIOSubscriptionApi(webServer, path: "/ws");
        }

        public IWebApi WebApi => this.webApi;
        public ISubscriptionApi SubscriptionApi => this.subscriptionApi;

        public void Start() {
            // Start both servers
            this.webApi.Compile();
            this.subscriptionApi.Start();

            // Start the underlying EmbedIOServer
            this.webServer.RunAsync();
        }

        public void Dispose() {
            this.webApi.Dispose();
            this.subscriptionApi.Dispose();
        }
    }
}
