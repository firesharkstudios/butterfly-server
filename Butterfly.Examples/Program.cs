using System;
using System.IO;

using NLog;
using Unosquare.Labs.EmbedIO.Modules;

using Butterfly.Util;

namespace Butterfly.Examples {
    class Program {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        static void Main(string[] args) {
            // Determine port and static files path
            var switches = CommandLineUtil.Parse(Environment.CommandLine);
            int port = switches.GetAs("port", 8080);
            string staticPath = switches.GetAs("static-path", @"..\..\..\Butterfly.Client.Web");
            string staticFullPath = staticPath.StartsWith(".") ? Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, staticPath)) : staticPath;
            logger.Debug($"Main():port={port},staticFullPath={staticFullPath}");

            // Create the underlying EmbedIOWebServer (see https://github.com/unosquare/embedio)
            var embedIOWebServer = new Unosquare.Labs.EmbedIO.WebServer(port);
            embedIOWebServer.RegisterModule(new StaticFilesModule(staticFullPath, headers: new System.Collections.Generic.Dictionary<string, string> {
                ["Cache-Control"] = "no-cache, no-store, must-revalidate",
                ["Pragma"] = "no-cache",
                ["Expires"] = "0"
            }));

            // Setup and start a webApiServer and channelServer using embedIOWebServer
            using (var webApiServer = new Butterfly.WebApi.EmbedIO.EmbedIOWebApiServer(embedIOWebServer))
            using (var channelServer = new Butterfly.Channel.EmbedIO.EmbedIOChannelServer(embedIOWebServer)) {
                // Setup each example (should each listen on unique URL paths for both webApiServer and channelServer)
                HelloWorldExample.Setup(webApiServer, channelServer);
                MinimalChatExample.Setup(webApiServer, channelServer);
                BetterChatExample.Setup(webApiServer, channelServer);

                // Start both servers
                webApiServer.Start();
                channelServer.Start();

                // Start the underlying EmbedIOServer
                embedIOWebServer.RunAsync().Wait();

                Console.WriteLine($"Open http://<host>:{port} to view");
            }
        }
    }

}
