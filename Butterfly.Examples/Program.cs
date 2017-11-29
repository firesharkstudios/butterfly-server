using System;
using System.IO;

using Butterfly.Util;
using NLog;

namespace Butterfly.Examples {
    class Program {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        static void Main(string[] args) {
            var switches = CommandLineUtil.Parse(Environment.CommandLine);
            int port = switches.GetAs("port", 8080);
            string staticPath = switches.GetAs("static-path", @"..\..\..\Butterfly.Client.Web");
            string staticFullPath = staticPath.StartsWith(".") ? Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, staticPath)) : staticPath;
            logger.Debug($"Main():port={port},staticFullPath={staticFullPath}");

            var redHttpServer = new RedHttpServerNet45.RedHttpServer(port, staticFullPath);
            using (var webServer = new Butterfly.Web.RedHttpServer.RedHttpServerWebServer(redHttpServer))
            using (var channelServer = new Butterfly.Channel.RedHttpServer.RedHttpServerChannelServer(redHttpServer)) {
                FullChatExample.Setup(webServer, "/api/full-chat", channelServer, "/full-chat");
                SimpleChatExample.Setup(webServer, "/api/simple-chat", channelServer, "/simple-chat");

                webServer.Start();
                channelServer.Start();
                redHttpServer.Start();
                Console.WriteLine($"Open http://<host>:{port} to view");
            }
        }
    }

}
