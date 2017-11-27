using System;
using System.IO;

using NLog;

using Butterfly.Channel;
using Butterfly.Util;

namespace Butterfly.Examples {
    class Program {
        protected static readonly Logger logger = LogManager.GetCurrentClassLogger();

        static void Main(string[] args) {
            int port = 8080;
            RedHttpServerNet45.RedHttpServer redHttpServer = new RedHttpServerNet45.RedHttpServer(port, Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, @"..\..\..\Butterfly.WebClient")));
            Butterfly.WebServer.WebServer webServer = new Butterfly.RedHttpServer.RedHttpServerWebServer(redHttpServer);
            ChannelServer channelServer = new Butterfly.RedHttpServer.RedHttpServerChannelServer(redHttpServer);

            FullChatExample.Setup(webServer, "/api/full-chat", channelServer, "/full-chat");
            SimpleChatExample.Setup(webServer, "/api/simple-chat", channelServer, "/simple-chat");

            webServer.Start();
            channelServer.Start();
            Console.WriteLine($"Open http://localhost:{port}/examples/index.html to view");
            ConsoleUtil.WaitForCancelKey();
            webServer.Stop();
            channelServer.Stop();
        }

    }

}
