using NLog;

using Butterfly.Channel;
using Butterfly.WebApi;

namespace Butterfly.Examples {
    public static class HelloWorldExample {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public static void Setup(IWebApiServer webApiServer, string apiPathPrefix, IChannelServer channelServer, string channelPathPrefix) {
            logger.Debug($"Setup():apiPathPrefix={apiPathPrefix},channelPathPrefix={channelPathPrefix}");

            // Setup database
            var database = new Butterfly.Database.Memory.MemoryDatabase();
            database.CreateFromTextAsync(@"CREATE TABLE message (
	            id INT NOT NULL AUTO_INCREMENT,
	            text VARCHAR(40) NOT NULL,
	            PRIMARY KEY (id)
            );").Wait();

            // Listen for clients creating new channels to /hello-world (each client
            // is expected to create and maintain a single channel to the server)
            channelServer.OnNewChannel(channelPathPrefix, channel => {
                // When a channel is created, create a dynamic view on the message table
                // and send all data events to the client over the channel
                return database.CreateAndStartDynamicView(
                    "message",
                    dataEventTransaction => {
                        channel.Queue(dataEventTransaction);
                    }
                );
            });

            // Listen for POST requests to /api/hello-world/message
            webApiServer.OnPost($"{apiPathPrefix}/message", async (req, res) => {
                // Parse the received message
                var message = await req.ParseAsJsonAsync<dynamic>();

                // INSERT a record into the message table (this will trigger 
                // any DynamicViews with a matching filter criteria to also 
                // publish an INSERT event)
                await database.InsertAndCommitAsync("message", new {
                    text = message
                });
            });
        }
    }
}