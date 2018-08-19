using System;
using System.Threading;

namespace Butterfly.Example.HelloWorld.Server {
    class Program {
        static ManualResetEvent quitEvent = new ManualResetEvent(false);

        static void Main(string[] args) {
            Console.CancelKeyPress += (sender, eArgs) => {
                quitEvent.Set();
                eArgs.Cancel = true;
            };

            // Create the underlying EmbedIOWebServer (see https://github.com/unosquare/embedio)
            var embedIOWebServer = new Unosquare.Labs.EmbedIO.WebServer(8000);
            Unosquare.Swan.Terminal.Settings.DisplayLoggingMessageType = Unosquare.Swan.LogMessageType.Info;

            // Create a MemoryDatabase (no persistence, limited features)
            var database = new Butterfly.Core.Database.Memory.MemoryDatabase();

            // Setup and start a webApiServer and channelServer using embedIOWebServer
            using (var webApiServer = new Butterfly.EmbedIO.EmbedIOWebApiServer(embedIOWebServer))
            using (var channelServer = new Butterfly.EmbedIO.EmbedIOChannelServer(embedIOWebServer, path: "/ws")) {
                Setup.Init(database, webApiServer, channelServer);

                webApiServer.Start();
                channelServer.Start();

                embedIOWebServer.RunAsync();

                Console.WriteLine("Open http://localhost:8000/ in a browser");
                quitEvent.WaitOne();
            }
        }

    }
}
