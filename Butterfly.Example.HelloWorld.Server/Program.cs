using System;
using System.Threading;

using NLog;

using Butterfly.Core.Channel;
using Butterfly.Core.WebApi;
using Unosquare.Labs.EmbedIO.Modules;
using Butterfly.Core.Database;

namespace Butterfly.Example.HelloWorld.Server {
    class Program {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        static ManualResetEvent quitEvent = new ManualResetEvent(false);

        static void Main(string[] args) {
            Console.CancelKeyPress += (sender, eArgs) => {
                quitEvent.Set();
                eArgs.Cancel = true;
            };

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
            database.CreateFromText(@"CREATE TABLE message (
	            id INT NOT NULL AUTO_INCREMENT,
	            text VARCHAR(40) NOT NULL,
	            PRIMARY KEY (id)
            );");

            // Setup and start a webApiServer and channelServer using embedIOWebServer
            using (var webApiServer = new Butterfly.EmbedIO.EmbedIOWebApiServer(embedIOWebServer))
            using (var channelServer = new Butterfly.EmbedIO.EmbedIOChannelServer(embedIOWebServer)) {
                // Setup each example (should each listen on unique URL paths for both webApiServer and channelServer)
                Setup(database, webApiServer, channelServer);

                // Start both servers
                webApiServer.Start();
                channelServer.Start();

                logger.Info($"Open http://localhost:{port}/ in a browser");

                // Start the underlying EmbedIOServer
                embedIOWebServer.RunAsync();

                try {
                    quitEvent.WaitOne();
                }
                finally {
                }
                logger.Debug("Main():Exiting...");
            }
        }

        public static void Setup(IDatabase database, IWebApiServer webApiServer, IChannelServer channelServer) {
            // Listen for API requests
            webApiServer.OnPost($"/api/message/insert", async (req, res) => {
                var text = await req.ParseAsJsonAsync<dynamic>();
                await database.InsertAndCommitAsync<long>("message", new {
                    text
                });
            });

            // Listen for websocket connections to /ws
            var route = channelServer.RegisterRoute("/ws");

            // Register a channel that creates a DynamicView on the message table 
            // (sends all the initial data in the message table and sends changes to the message table)
            route.RegisterChannel(
                channelKey: "my-channel", 
                handlerAsync: async (vars, channel) => await database.CreateAndStartDynamicView(
                    "message",
                    listener: dataEventTransaction => channel.Queue(dataEventTransaction)
                )
            );
        }

    }
}
