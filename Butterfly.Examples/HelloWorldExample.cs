using NLog;

using Butterfly.Channel;
using Butterfly.WebApi;
using Butterfly.Database.Dynamic;

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
            var route = channelServer.RegisterRoute("/hello-world");

            // Register a default channel that creates a DynamicView on the message table sending all data to the channel
            route.RegisterChannel(handlerAsync: async (vars, channel) => await database.CreateAndStartDynamicView(
                "SELECT * FROM message",
                dataEventTransaction => channel.Queue(dataEventTransaction)
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