using System;
using System.IO;

namespace Butterfly.Examples {
    class Program {
        static void Main(string[] args) {
            int port = 8080;
            var redHttpServer = new RedHttpServerNet45.RedHttpServer(port, Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, @"..\..\..\Butterfly.Client.Web")));
            using (var webServer = new Butterfly.RedHttpServer.RedHttpServerWebServer(redHttpServer))
            using (var channelServer = new Butterfly.RedHttpServer.RedHttpServerChannelServer(redHttpServer)) {
                FullChatExample.Setup(webServer, "/api/full-chat", channelServer, "/full-chat");
                SimpleChatExample.Setup(webServer, "/api/simple-chat", channelServer, "/simple-chat");

                webServer.Start();
                channelServer.Start();
                Console.WriteLine($"Open http://localhost:{port}/examples/index.html to view");
            }
        }
    }

}
