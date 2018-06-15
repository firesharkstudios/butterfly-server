using System;
using System.Reflection;

using NLog;

using Butterfly.Core.Channel;
using Butterfly.Core.Database;
using Butterfly.Core.Util;
using Butterfly.Core.WebApi;

namespace Butterfly.Examples {
    public static class BetterChatExample {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        // This is called by Program.cs
        public static void Setup(IWebApiServer webApiServer, IChannelServer channelServer) {
            logger.Debug($"Setup()");

            // Setup a MySQL database (may need to execute "GRANT ALL PRIVILEGES ON butterfly_better_chat.* TO 'test'@'localhost' IDENTIFIED BY 'test!123'; CREATE DATABASE butterfly_chat;")
            IDatabase database = null;
            try {
                database = new Butterfly.MySql.MySqlDatabase("Server=127.0.0.1;Uid=test;Pwd=test!123;Database=butterfly_better_chat");
            }
            catch (UnableToConnectDatabaseException) {
                logger.Warn($"Unable to connect to MySQL server (modify BetterChatExample.cs to specify ConnectionString), skipping BetterChatExample");
                return;
            }
            database.CreateFromResourceFile(Assembly.GetExecutingAssembly(), "Butterfly.Examples.better-chat-db.sql");
            database.SetDefaultValue("id", tableName => Guid.NewGuid().ToString());
            database.SetDefaultValue("created_at", tableName => DateTime.Now);
            database.SetDefaultValue("updated_at", tableName => DateTime.Now);
            database.SetDefaultValue("join_id", tableName => Guid.NewGuid().ToString().Substring(0, 8), "chat");

            // Listen for connections to /better-chat
            var route = channelServer.RegisterRoute(
                "/better-chat",
                getAuthTokenAsync: async (authType, authValue) => {
                    // Create a user record if missing
                    await database.InsertAndCommitAsync<string>("user", new {
                        id = authValue,
                        name = CleverNameX.Generate(),
                    }, ignoreIfDuplicate: true);

                    return authValue;
                },
                getConnectionId: authToken => Guid.NewGuid().ToString()
            );

            // Register a default channel that creates all DynamicView instances needed and sends all data to the channel
            route.RegisterChannel(handlerAsync: async (vars, channel) => {
                // Create a DynamicViewSet that sends DataEventTransactions to the channel
                var dynamicViewSet = database.CreateDynamicViewSet(dataEventTransaction => {
                    channel.Queue("DATA-EVENT-TRANSACTION", dataEventTransaction);
                });

                // Add a "me" DynamicView that includes the user's record
                dynamicViewSet.CreateDynamicView(
                    @"SELECT id, name 
                    FROM user 
                    WHERE id=@userId", 
                    values: new {
                        userId = channel.Connection.AuthToken
                    },
                    name: "me"
                );

                // Add a "chat" DynamicView that includes all the chats the user can see
                dynamicViewSet.CreateDynamicView(
                    @"SELECT c.id, c.name, c.join_id, c.owner_id, u.name owner_name 
                    FROM chat c 
                        INNER JOIN user u ON c.owner_id=u.id
                        INNER JOIN chat_participant cpx ON c.id=cpx.chat_id
                    WHERE cpx.user_id=@userId",
                    values: new {
                        userId = channel.Connection.AuthToken
                    }
                );

                // Add a "chat_participant" DynamicView that includes all the chat participants the user can see
                dynamicViewSet.CreateDynamicView(
                    @"SELECT cp.id, cp.chat_id, u.id user_id, u.name 
                    FROM chat_participant cp 
                        INNER JOIN user u ON cp.user_id=u.id
                        INNER JOIN chat_participant cpx ON cp.chat_id=cpx.chat_id
                    WHERE cpx.user_id=@userId",
                    values: new {
                        userId = channel.Connection.AuthToken
                    }
                );

                // Add a "chat_message" DynamicView that includes all the chat messages the user can see
                dynamicViewSet.CreateDynamicView(
                    @"SELECT cm.id, cm.chat_id, cm.text, cm.created_at, u.id user_id, u.name 
                    FROM chat_message cm 
                        INNER JOIN user u ON cm.user_id=u.id
                        INNER JOIN chat_participant cpx ON cm.chat_id=cpx.chat_id
                    WHERE cpx.user_id=@userId",
                    values: new {
                        userId = channel.Connection.AuthToken
                    }
                );

                // Start the DynamicView group (executes all the DynamicView queries and sends changes as they occur)
                await dynamicViewSet.StartAsync();

                return dynamicViewSet;
            });

            // Listen for API requests to /api/profile/update
            webApiServer.OnPost($"/api/better-chat/profile/update", async (req, res) => {
                logger.Debug("Main():/api/profile/update");
                var auth = req.GetAuthenticationHeaderValue();
                var user = await req.ParseAsJsonAsync<dynamic>();

                // Update record in database
                await database.UpdateAndCommitAsync("UPDATE user SET name=@name WHERE id=@id", new {
                    id = auth.Parameter,
                    user.name,
                });
            });

            // Listen for API requests to /api/chat/create
            webApiServer.OnPost($"/api/better-chat/chat/create", async(req, res) => {
                logger.Debug("Main():/api/chat/create");
                var auth = req.GetAuthenticationHeaderValue();
                var chat = await req.ParseAsJsonAsync<dynamic>();

                // Create records in database
                using (var transaction = await database.BeginTransactionAsync()) {
                    string chatId = await transaction.InsertAsync<string>("chat", new {
                        chat.name,
                        owner_id = auth.Parameter
                    });
                    await transaction.InsertAsync<string>("chat_participant", new {
                        chat_id = chatId,
                        user_id = auth.Parameter,
                    });
                    await transaction.CommitAsync();
                }
            });

            // Listen for API requests to /api/chat/join
            webApiServer.OnPost($"/api/better-chat/chat/join", async (req, res) => {
                logger.Debug("Main():/api/chat/join");
                var auth = req.GetAuthenticationHeaderValue();
                var join = await req.ParseAsJsonAsync<dynamic>();

                var chatId = await database.SelectValueAsync<string>("SELECT id FROM chat WHERE join_id=@joinId", new {
                    join.joinId,
                }, null);

                // Create records in database
                if (chatId != null) {
                    await database.InsertAndCommitAsync<string>("chat_participant", new {
                        chat_id = chatId,
                        user_id = auth.Parameter,
                    }, ignoreIfDuplicate: true);
                }
            });

            // Listen for API requests to /api/chat/delete
            webApiServer.OnPost($"/api/better-chat/chat/delete", async (req, res) => {
                logger.Debug("Main():/api/chat/delete");
                var auth = req.GetAuthenticationHeaderValue();
                var chat = await req.ParseAsJsonAsync<dynamic>();

                // Create records in database
                await database.DeleteAndCommitAsync("DELETE FROM chat WHERE id=@chatId", new {
                    chatId = chat.id
                });
            });

            // Listen for API requests to /api/chat/message
            webApiServer.OnPost($"/api/better-chat/chat/message", async(req, res) => {
                var auth = req.GetAuthenticationHeaderValue();
                var chatMessage = await req.ParseAsJsonAsync<dynamic>();

                // Create record in database
                await database.InsertAndCommitAsync<string>("chat_message", new {
                    user_id = auth.Parameter,
                    chat_id = chatMessage.chatId,
                    chatMessage.text
                });
            });
        }
    }
}
