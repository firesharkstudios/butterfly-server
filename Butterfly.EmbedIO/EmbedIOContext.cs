/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;

using Unosquare.Labs.EmbedIO.Modules;

using Butterfly.Core.Channel;
using Butterfly.Core.WebApi;
using Butterfly.Core.Util;

using NLog;

namespace Butterfly.EmbedIO {
    /// <summary>
    /// Convenient class to initialize an IWebApiServer and IChannelServer instance from a running EmbedIO.WebServer instance
    /// </summary>
    public class EmbedIOContext : IDisposable {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        protected readonly Unosquare.Labs.EmbedIO.WebServer embedIOWebServer;
        protected readonly IWebApiServer webApiServer;
        protected readonly IChannelServer channelServer;

        public EmbedIOContext(string url, string staticPath = null) {
            // Binding to all local IP addresses requires adding an HTTP URL ACL rule
            // This may prompt to "allow app to modify your device"
            ProcessX.AddHttpUrlAclIfNeeded(url);

            // Create the underlying EmbedIOWebServer (see https://github.com/unosquare/embedio)
            this.embedIOWebServer = new Unosquare.Labs.EmbedIO.WebServer(url);
            if (!string.IsNullOrEmpty(staticPath)) {
                logger.Debug($"EmbedIOContext():staticPath={staticPath}");
                this.embedIOWebServer.RegisterModule(new StaticFilesModule(staticPath, headers: new System.Collections.Generic.Dictionary<string, string> {
                    ["Cache-Control"] = "no-cache, no-store, must-revalidate",
                    ["Pragma"] = "no-cache",
                    ["Expires"] = "0"
                }));
            }
            //Unosquare.Swan.Terminal.Settings.DisplayLoggingMessageType = Unosquare.Swan.LogMessageType.Trace;

            // Setup and start a webApiServer and channelServer using embedIOWebServer
            this.webApiServer = new EmbedIOWebApiServer(embedIOWebServer);
            this.channelServer = new EmbedIOChannelServer(embedIOWebServer, path: "/ws");
        }

        public IWebApiServer WebApiServer => this.webApiServer;
        public IChannelServer ChannelServer => this.channelServer;

        public void Start() {
            // Start both servers
            this.webApiServer.Start();
            this.channelServer.Start();

            // Start the underlying EmbedIOServer
            this.embedIOWebServer.RunAsync();
        }

        public void Dispose() {
            this.webApiServer.Dispose();
            this.channelServer.Dispose();
        }
    }
}
