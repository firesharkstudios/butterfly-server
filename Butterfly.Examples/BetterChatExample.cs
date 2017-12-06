using System;
using System.Reflection;

using NLog;

using Butterfly.Channel;
using Butterfly.Util;
using Butterfly.WebApi;
using Butterfly.Database.Event;

namespace Butterfly.Examples {
    public static class BetterChatExample {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        // This is called by Program.cs
        public static void Setup(IWebApiServer webApiServer, IChannelServer channelServer) {
            logger.Debug($"Setup()");

            // Setup a MySQL database (may need to execute "GRANT ALL PRIVILEGES ON *.* TO 'test'@'localhost' IDENTIFIED BY 'test!123'; CREATE DATABASE butterfly_chat;")
            var database = new Butterfly.Database.MySql.MySqlDatabase("Server=127.0.0.1;Uid=test;Pwd=test!123;Database=butterfly_chat");
            database.CreateFromResourceFileAsync(Assembly.GetExecutingAssembly(), "Butterfly.Examples.better-chat-db.sql").Wait();
            database.SetInsertDefaultValue("id", () => Guid.NewGuid().ToString());
            database.SetInsertDefaultValue("created_at", () => DateTime.Now);
            database.SetInsertDefaultValue("updated_at", () => DateTime.Now);
            database.SetInsertDefaultValue("join_id", () => Guid.NewGuid().ToString().Substring(0, 8), "chat");

            // Listen for clients creating new channels to /better-chat
            // (clients are expected to maintain a channel to the server)
            channelServer.OnNewChannelAsync("/better-chat", async(channel) => {
                // Create a user record if missing
                await database.InsertAndCommitAsync("user", new {
                    id = channel.AuthId,
                    name = CleverNameX.Generate(),
                }, ignoreIfDuplicate: true);

                // Create a DynamicViewSet that sends DataEventTransactions to the channel
                var dynamicViewSet = database.CreateDynamicViewSet(
                    listener: dataEventTransaction => {
                        var filteredDataEventTransaction = DataEventTransaction.FilterDataEvents(dataEventTransaction, dataEvent => dataEvent.name != "chat_ids");
                        channel.Queue(filteredDataEventTransaction);
                    }
                );

                // Add a "me" dynamic select that includes the user's record
                dynamicViewSet.CreateDynamicView(
                    "SELECT id, name FROM user WHERE id=@userId", 
                    values: new {
                        userId = channel.AuthId
                    },
                    name: "me"
                );

                // Build a dynamic list of chat ids for the user
                var chatIdsDynamicView = dynamicViewSet.CreateDynamicView(
                    "SELECT id, chat_id FROM chat_participant WHERE user_id=@userId", 
                    values: new {
                        userId = channel.AuthId
                    }, 
                    name: "chat_ids"
                );
                var chatIds = chatIdsDynamicView.CreateMultiValueDynamicParam("chat_id");

                // Add a "chat" dynamic select that includes all the chats the user can see
                dynamicViewSet.CreateDynamicView(
                    @"SELECT c.id, c.name, c.join_id, c.owner_id, u.name owner_name 
                      FROM chat c INNER JOIN user u ON c.owner_id=u.id
                      WHERE c.id=@chatIds",
                    values: new {
                        chatIds = chatIds
                    },
                    name: "chat",
                    keyFieldNames: new string[] { "id" }
                );

                // Add a "chat_participant" dynamic select that includes all the chat participatns the user can see
                dynamicViewSet.CreateDynamicView(
                    @"SELECT cp.id, cp.chat_id, u.id user_id, u.name 
                      FROM chat_participant cp INNER JOIN user u ON cp.user_id=u.id
                      WHERE cp.chat_id=@chatIds", 
                    values: new {
                        chatIds = chatIds
                    },
                    name: "chat_participant",
                    keyFieldNames: new string[] { "id" }
                );

                // Add a "chat_message" dynamic select that includes all the chat messages the user can see
                dynamicViewSet.CreateDynamicView(
                    @"SELECT cm.id, cm.chat_id, cm.text, cm.created_at, u.id user_id, u.name 
                      FROM chat_message cm INNER JOIN user u ON cm.user_id=u.id
                      WHERE cm.chat_id=@chatIds",
                    values: new {
                        chatIds = chatIds
                    },
                    name: "chat_message",
                    keyFieldNames: new string[] { "id" }
                );

                // Start the dynamic select group (executes all the dynamic select queries and sends changes as they occur)
                return await dynamicViewSet.StartAsync();
            });

            // Listen for API requests to /api/profile/update
            webApiServer.OnPost($"/api/better-chat/profile/update", async (req, res) => {
                logger.Debug("Main():/api/profile/update");
                var auth = req.AuthenticationHeaderValue;
                var user = await req.ParseAsJsonAsync<dynamic>();

                // Update record in database
                await database.UpdateAndCommitAsync("UPDATE user SET name=@name WHERE id=@id", new {
                    id = auth.Parameter,
                    name = user.name,
                });
            });

            // Listen for API requests to /api/chat/create
            webApiServer.OnPost($"/api/better-chat/chat/create", async(req, res) => {
                logger.Debug("Main():/api/chat/create");
                var auth = req.AuthenticationHeaderValue;
                var chat = await req.ParseAsJsonAsync<dynamic>();

                // Create records in database
                using (var transaction = await database.BeginTransaction()) {
                    object chatId = await transaction.InsertAsync("INSERT INTO chat (@@names) VALUES (@@values)", new {
                        name = chat.name,
                        owner_id = auth.Parameter
                    });
                    await transaction.InsertAsync("INSERT INTO chat_participant (@@names) VALUES (@@values)", new {
                        chat_id = chatId,
                        user_id = auth.Parameter,
                    });
                    await transaction.CommitAsync();
                }
            });

            // Listen for API requests to /api/chat/join
            webApiServer.OnPost($"/api/better-chat/chat/join", async (req, res) => {
                logger.Debug("Main():/api/chat/join");
                var auth = req.AuthenticationHeaderValue;
                var join = await req.ParseAsJsonAsync<dynamic>();

                var chatId = await database.SelectValue<string>("SELECT id FROM chat WHERE join_id=@joinId", new {
                    joinId = join.joinId,
                }, null);

                // Create records in database
                if (chatId != null) {
                    await database.InsertAndCommitAsync("INSERT INTO chat_participant (@@names) VALUES (@@values)", new {
                        chat_id = chatId,
                        user_id = auth.Parameter,
                    }, ignoreIfDuplicate: true);
                }
            });

            // Listen for API requests to /api/chat/delete
            webApiServer.OnPost($"/api/better-chat/chat/delete", async (req, res) => {
                logger.Debug("Main():/api/chat/delete");
                var auth = req.AuthenticationHeaderValue;
                var chat = await req.ParseAsJsonAsync<dynamic>();

                // Create records in database
                await database.DeleteAndCommitAsync("DELETE FROM chat WHERE id=@chatId", new {
                    chatId = chat.id
                });
            });

            // Listen for API requests to /api/chat/message
            webApiServer.OnPost($"/api/better-chat/chat/message", async(req, res) => {
                var auth = req.AuthenticationHeaderValue;
                var chatMessage = await req.ParseAsJsonAsync<dynamic>();

                // Create record in database
                await database.InsertAndCommitAsync("INSERT INTO chat_message (@@names) VALUES (@@values)", new {
                    user_id = auth.Parameter,
                    chat_id = chatMessage.chatId,
                    text = chatMessage.text
                });
            });
        }
    }
}
