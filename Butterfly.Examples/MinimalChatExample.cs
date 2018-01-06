using System;

using NLog;

using Butterfly.Channel;
using Butterfly.Database.Dynamic;
using Butterfly.WebApi;

namespace Butterfly.Examples {
    public static class MinimalChatExample {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        // This is called by Program.cs
        public static void Setup(IWebApiServer webApiServer, IChannelServer channelServer) {
            logger.Debug($"Setup()");

            // Create a MemoryDatabase (no persistence, limited features)
            // with a single table "chat_message"
            var database = new Butterfly.Database.Memory.MemoryDatabase();
            database.CreateFromText(@"CREATE TABLE chat_message (
	            id INT NOT NULL AUTO_INCREMENT,
	            user_name VARCHAR(40) NOT NULL,
	            text VARCHAR(255) NOT NULL,
	            created_at DATETIME NOT NULL,
	            PRIMARY KEY (id)
            );");
            database.SetInsertDefaultValue("created_at", tableName => DateTime.Now);

            // Listen for clients creating new channels to /minimal-chat,
            // clients are expected to maintain a channel to the server,
            // channels are currently implemented over WebSockets
            //
            // When a channel is created, create a DynamicView on the message table sending all 
            // initial data and data changes to the client over the channel
            channelServer.OnNewChannel("/minimal-chat", async(authType, authValue, channel) => {
                DynamicViewSet dynamicViewSet = await database.CreateAndStartDynamicView(
                    "chat_message",
                    dataEventTransaction => {
                        channel.Queue(dataEventTransaction);
                    }
                );
                channel.Attach(dynamicViewSet);
                return "some-unique-channel-id";
            });

            // Listen for API requests to /api/chat/message
            webApiServer.OnPost($"/api/minimal-chat/chat/message", async(req, res) => {
                // Parse the received message
                var chatMessage = await req.ParseAsJsonAsync<dynamic>();

                // INSERT a record into the chat_message table (triggers any DynamicViews 
                // with matching criteria to also publish an INSERT event)
                await database.InsertAndCommitAsync<long>("chat_message", new {
                    user_name = chatMessage.userName,
                    chatMessage.text
                });
            });
        }
    }
}