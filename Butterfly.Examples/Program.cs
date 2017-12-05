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

            // Create EmbedIOServer
            var embedIOWebServer = new Unosquare.Labs.EmbedIO.WebServer(port);
            embedIOWebServer.RegisterModule(new StaticFilesModule(staticFullPath));

            // Configure and start web server and channel server
            using (var webServer = new Butterfly.WebApi.EmbedIO.EmbedIOWebApiServer(embedIOWebServer))
            using (var channelServer = new Butterfly.Channel.EmbedIO.EmbedIOChannelServer(embedIOWebServer)) {
                HelloWorldExample.Setup(webServer, channelServer);
                MinimalChatExample.Setup(webServer, channelServer);
                FullChatExample.Setup(webServer, channelServer);
                webServer.Start();
                channelServer.Start();

                embedIOWebServer.RunAsync().Wait();

                Console.WriteLine($"Open http://<host>:{port} to view");
            }
        }
    }

}
