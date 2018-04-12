using NLog;

using Butterfly.Channel;
using Butterfly.WebApi;
using Butterfly.Database.Dynamic;
using System;

namespace Butterfly.Examples {
    public static class HelloWorldExample {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        // This is called by Program.cs
        public static void Setup(IWebApiServer webApiServer, IChannelServer channelServer) {
            logger.Debug($"Setup()");

            // Create a MemoryDatabase (no persistence, limited features)
            var database = new Butterfly.Database.Memory.MemoryDatabase();
            database.CreateFromText(@"CREATE TABLE message (
	            id INT NOT NULL AUTO_INCREMENT,
	            text VARCHAR(40) NOT NULL,
	            PRIMARY KEY (id)
            );");

            // Listen for websocket connections to /hello-world
            var route = channelServer.RegisterRoute(
                "/hello-world",
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