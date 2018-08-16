using System;
using System.Threading;

using NLog;

using Butterfly.Core.Channel;
using Butterfly.Core.WebApi;
using Unosquare.Labs.EmbedIO.Modules;

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

            // Setup and start a webApiServer and channelServer using embedIOWebServer
            using (var webApiServer = new Butterfly.EmbedIO.EmbedIOWebApiServer(embedIOWebServer))
            using (var channelServer = new Butterfly.EmbedIO.EmbedIOChannelServer(embedIOWebServer)) {
                // Setup each example (should each listen on unique URL paths for both webApiServer and channelServer)
                Setup(webApiServer, channelServer);

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

        public static void Setup(IWebApiServer webApiServer, IChannelServer channelServer) {
            logger.Debug($"Setup()");

            // Create a MemoryDatabase (no persistence, limited features)
            var database = new Butterfly.Core.Database.Memory.MemoryDatabase();
            database.CreateFromText(@"CREATE TABLE message (
	            id INT NOT NULL AUTO_INCREMENT,
	            text VARCHAR(40) NOT NULL,
	            PRIMARY KEY (id)
            );");

            // Listen for websocket connections to /hello-world
            var route = channelServer.RegisterRoute(
                "/ws",
                getAuthToken: (authType, authValue) => "OK",
                getConnectionId: authToken => Guid.NewGuid().ToString()
            );

            // Register a default channel that creates a DynamicView on the message table sending all data to the channel
            route.RegisterChannel(channelKey: "my-channel", handlerAsync: async (vars, channel) => await database.CreateAndStartDynamicView(
                sql: "SELECT * FROM message",
                listener: dataEventTransaction => channel.Queue("DATA-EVENT-TRANSACTION", dataEventTransaction)
            ));

            // Listen for POST requests to /api/hello-world/message
            webApiServer.OnPost($"/api/hello-world/message", async (req, res) => {
                // Parse the received message
                var message = await req.ParseAsJsonAsync<dynamic>();

                // INSERT a record into the chat_message table (triggers any DynamicViews 
                // with matching criteria to also publish an INSERT event)
                await database.InsertAndCommitAsync<long>("message", new {
                    text = message
                });
            });
        }

    }
}
