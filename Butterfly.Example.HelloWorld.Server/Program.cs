using System;

using Unosquare.Labs.EmbedIO.Modules;

namespace Butterfly.Example.HelloWorld.Server {
    class Program {
        static void Main(string[] args) {
            int port = 8080;
            string staticFullPath = "../../../Butterfly.Example.HelloWorld.Client/";

            // Create the underlying EmbedIOWebServer (see https://github.com/unosquare/embedio)
            var embedIOWebServer = new Unosquare.Labs.EmbedIO.WebServer(port);
            embedIOWebServer.RegisterModule(new StaticFilesModule(staticFullPath, headers: new System.Collections.Generic.Dictionary<string, string> {
                ["Cache-Control"] = "no-cache, no-store, must-revalidate",
                ["Pragma"] = "no-cache",
                ["Expires"] = "0"
            }));
            Unosquare.Swan.Terminal.Settings.DisplayLoggingMessageType = Unosquare.Swan.LogMessageType.Info;

            // Create a MemoryDatabase (no persistence, limited features)
            var database = new Butterfly.Core.Database.Memory.MemoryDatabase();

            // Setup and start a webApiServer and channelServer using embedIOWebServer
            using (var webApiServer = new Butterfly.EmbedIO.EmbedIOWebApiServer(embedIOWebServer))
            using (var channelServer = new Butterfly.EmbedIO.EmbedIOChannelServer(embedIOWebServer, path: "/ws")) {
                // Setup each example (should each listen on unique URL paths for both webApiServer and channelServer)
                Setup.Init(database, webApiServer, channelServer);

                // Start both servers
                webApiServer.Start();
                channelServer.Start();

                // Start the underlying EmbedIOServer
                embedIOWebServer.RunAsync();

                Console.WriteLine($"Open http://localhost:{port}/ in a browser");
                Console.ReadLine();
            }
        }

    }
}
