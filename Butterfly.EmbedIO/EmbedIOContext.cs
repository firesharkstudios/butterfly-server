using System;

using Unosquare.Labs.EmbedIO.Modules;

using Butterfly.Core.Channel;
using Butterfly.Core.WebApi;

namespace Butterfly.EmbedIO {
    /// <summary>
    /// Convenient class to initialize an IWebApiServer and IChannelServer instance from a running EmbedIO.WebServer instance
    /// </summary>
    public class EmbedIOContext : IDisposable {
        protected readonly Unosquare.Labs.EmbedIO.WebServer embedIOWebServer;
        protected readonly IWebApiServer webApiServer;
        protected readonly IChannelServer channelServer;

        public EmbedIOContext(int port = 8080, string staticFullPath = null) {
            // Create the underlying EmbedIOWebServer (see https://github.com/unosquare/embedio)
            this.embedIOWebServer = new Unosquare.Labs.EmbedIO.WebServer(port);
            if (!string.IsNullOrEmpty(staticFullPath)) {
                this.embedIOWebServer.RegisterModule(new StaticFilesModule(staticFullPath, headers: new System.Collections.Generic.Dictionary<string, string> {
                    ["Cache-Control"] = "no-cache, no-store, must-revalidate",
                    ["Pragma"] = "no-cache",
                    ["Expires"] = "0"
                }));
            }
            Unosquare.Swan.Terminal.Settings.DisplayLoggingMessageType = Unosquare.Swan.LogMessageType.Info;

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
