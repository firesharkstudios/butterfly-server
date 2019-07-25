/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using Butterfly.Core.Channel;
using Butterfly.Core.Util;
using Butterfly.Core.WebApi;
using NLog;

namespace Butterfly.RedHttpServer {

    /// <summary>
    /// Convenient class to initialize IWebApi and ISubscriptionApi instances using RedHttpServer (see https://github.com/rosenbjerg/Red)
    /// </summary>
    public class RedHttpServerContext : IDisposable {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        protected readonly Red.RedHttpServer webServer;
        protected readonly IWebApi webApi;
        protected readonly ISubscriptionApi subscriptionApi;

        public RedHttpServerContext(string url, string staticPath = null) {
            // Binding to all local IP addresses requires adding an HTTP URL ACL rule
            // This may prompt to "allow app to modify your device"
            ProcessX.AddHttpUrlAclIfNeeded(url);

            // Create the underlying RedHttpServer (see https://github.com/rosenbjerg/Red)
            this.webServer = new Red.RedHttpServer(8000, staticPath);

            // Create the IWebApi and ISubscriptionApi wrappers
            this.webApi = new RedHttpServerWebApi(webServer);
            this.subscriptionApi = new RedHttpServerSubscriptionApi(webServer, path: "/ws");
        }

        public IWebApi WebApi => this.webApi;
        public ISubscriptionApi SubscriptionApi => this.subscriptionApi;

        public void Start() {
            // Start both servers
            this.webApi.Compile();
            this.subscriptionApi.Start();

            // Start the underlying RedHttpServer
            this.webServer.RunAsync();
        }

        public void Dispose() {
            this.webApi.Dispose();
            this.subscriptionApi.Dispose();
        }
    }
}
