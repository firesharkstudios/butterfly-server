using System;

using NLog;

using Butterfly.Channel;
using Butterfly.WebApi;

namespace Butterfly.Examples {
    public static class MinimalChatExample {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public static void Setup(IWebApiServer webApiServer, string apiPathPrefix, IChannelServer channelServer, string channelPathPrefix) {
            logger.Debug($"Setup():apiPathPrefix={apiPathPrefix},channelPathPrefix={channelPathPrefix}");

            // Setup database
            var database = new Butterfly.Database.Memory.MemoryDatabase();
            database.CreateFromTextAsync(@"CREATE TABLE chat_message (
	            id INT NOT NULL AUTO_INCREMENT,
	            user_name VARCHAR(40) NOT NULL,
	            text VARCHAR(255) NOT NULL,
	            created_at DATETIME NOT NULL,
	            PRIMARY KEY (id)
            );").Wait();
            database.SetInsertDefaultValue("created_at", () => DateTime.Now);

            // Send initial data events and subsequent data events to any new channels created
            channelServer.OnNewChannel(channelPathPrefix, channel => {
                var initialDataEventTransaction = database.GetInitialDataEventTransactionAsync("chat_message").Result;
                channel.Queue(initialDataEventTransaction);
                return database.OnNewCommittedTransaction(dataEventTransaction => {
                    channel.Queue(dataEventTransaction);
                });
            });

            // Listen for API requests to /api/chat/message
            webApiServer.OnPost($"{apiPathPrefix}/chat/message", async(req, res) => {
                var chatMessage = await req.ParseAsJsonAsync<dynamic>();
                await database.InsertAndCommitAsync("chat_message", new {
                    user_name = chatMessage.userName,
                    text = chatMessage.text
                });
            });
        }
    }
}