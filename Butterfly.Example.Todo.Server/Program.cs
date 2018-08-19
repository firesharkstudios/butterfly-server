using System;
using System.Reflection;
using System.Threading;

using Butterfly.Core.Util;

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
            database.CreateFromResourceFile(Assembly.GetExecutingAssembly(), "Butterfly.Example.Todo.Server.db.sql");
            database.SetDefaultValue("id", tableName => $"{tableName.Abbreviate()}_{Guid.NewGuid().ToString()}");
            database.SetDefaultValue("created_at", tableName => DateTime.Now);
            database.SetOverrideValue("updated_at", tableName => DateTime.Now);

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
