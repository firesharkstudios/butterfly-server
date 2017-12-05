using NLog;

using Butterfly.Channel;
using Butterfly.WebApi;

namespace Butterfly.Examples {
    public static class HelloWorldExample {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        // This is called by Program.cs
        public static void Setup(IWebApiServer webApiServer, IChannelServer channelServer) {
            logger.Debug($"Setup()");

            // Create a MemoryDatabase (no persistence, limited features)
            var database = new Butterfly.Database.Memory.MemoryDatabase();
            database.CreateFromTextAsync(@"CREATE TABLE message (
	            id INT NOT NULL AUTO_INCREMENT,
	            text VARCHAR(40) NOT NULL,
	            PRIMARY KEY (id)
            );").Wait();

            // Listen for clients creating new channels to /hello-world,
            // clients are expected to maintain a channel to the server,
            // channels are currently implemented over WebSockets
            //
            // When a channel is created, create a DynamicView on the message table sending all 
            // initial data and data changes to the client over the channel
            channelServer.OnNewChannel("/hello-world", channel => database.CreateAndStartDynamicView(
            "message",
                dataEventTransaction => {
                    channel.Queue(dataEventTransaction);
                }
            ));

            // Listen for POST requests to /api/hello-world/message
            webApiServer.OnPost($"/api/hello-world/message", async (req, res) => {
                // Parse the received message
                var message = await req.ParseAsJsonAsync<dynamic>();

                // INSERT a record into the chat_message table (triggers any DynamicViews 
                // with matching criteria to also publish an INSERT event)
                await database.InsertAndCommitAsync("message", new {
                    text = message
                });
            });
        }
    }
}