using System;
using System.Threading;

using NLog;
using Unosquare.Labs.EmbedIO.Modules;

using Butterfly.Core.Channel;
using Butterfly.Core.Database;
using Butterfly.Core.WebApi;
using Butterfly.Core.Util;

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
            database.SetDefaultValue("id", tableName => $"{tableName.Abbreviate()}_{Guid.NewGuid().ToString()}");
            database.SetDefaultValue("created_at", tableName => DateTime.Now.ToUnixTimestamp());
            database.SetOverrideValue("updated_at", tableName => DateTime.Now.ToUnixTimestamp());

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
            logger.Debug($"Setup()");

            // Listen for API requests
            webApiServer.OnPost($"/api/todo/insert", async (req, res) => {
                var todo = await req.ParseAsJsonAsync<dynamic>();
                await database.InsertAndCommitAsync<string>("todo", todo);
            });
            webApiServer.OnPost($"/api/todo/update", async (req, res) => {
                var todo = await req.ParseAsJsonAsync<dynamic>();
                await database.UpdateAndCommitAsync("todo", todo);
            });
            webApiServer.OnPost($"/api/todo/delete", async (req, res) => {
                var id = await req.ParseAsJsonAsync<string>();
                await database.DeleteAndCommitAsync("todo", id);
            });

            // Listen for websocket connections to /ws
            var route = channelServer.RegisterRoute("/ws");

            // Register a channel that creates a DynamicView on the todo table 
            // (sends all the initial data in the todo table and sends changes to the todo table)
            route.RegisterChannel(
                channelKey: "my-channel", 
                handlerAsync: async (vars, channel) => await database.CreateAndStartDynamicView(
                    "todo",
                    listener: dataEventTransaction => channel.Queue(dataEventTransaction)
                )
            ));
        }

    }
}
