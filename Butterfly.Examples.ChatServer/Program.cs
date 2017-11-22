/*
 * Copyright 2017 Fireshark Studios, LLC
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;

using NLog;
using RedHttpServerNet45;

using Butterfly.Channel;
using Butterfly.Database;
using Butterfly.Util;
using Butterfly.WebServer;
using Fireshark.Butterfly.Dynamic;

namespace ChatServer {
    class Program {
        protected static readonly Logger logger = LogManager.GetCurrentClassLogger();

        static void Main(string[] args) {
            // Setup database
            Database database = new Butterfly.Database.MySql.MySqlDatabase("Server=127.0.0.1;Uid=root;Pwd=test!123;Database=butterfly_chat");
            database.SetDefaultValue("id", () => Guid.NewGuid().ToString());
            database.SetDefaultValue("createdAt", () => DateTime.Now);
            database.SetDefaultValue("updatedAt", () => DateTime.Now);
            database.Tables["chat"].SetDefaultValue("join_id", () => Guid.NewGuid().ToString().Substring(0, 8));

            // Setup web server and channel manager
            RedHttpServer redHttpServer = new RedHttpServer(8080, @"\Users\Kent\Documents\Visual Studio 2017\Projects\Butterfly\Butterfly.Examples.ChatClient");
            BaseWebServer webServer = new Butterfly.RedHttpServer.RedHttpServerWebServer(redHttpServer);
            BaseChannelServer channelManager = new Butterfly.RedHttpServer.RedHttpServerChannelServer(redHttpServer);

            // Initialize new channels created
            channelManager.OnNewChannelAsync(async(channel) => {
                // Create a user record if missing
                await database.InsertAndCommitAsync("INSERT INTO user (@@names) VALUES (@@values)", new {
                    id = channel.Id,
                    name = CleverNameX.Generate(),
                }, ignoreIfDuplicate: true);

                // Create a dynamic select group that sends changes to the channel
                var dynamicSelectGroup = new DynamicSelectGroup(
                    database,
                    listenerDataEventFilter: dataEvent => dataEvent.name != "chat_ids",
                    listener:dataChangeTransaction => {
                        channel.Queue(dataChangeTransaction);
                    }
                );

                // Add a "me" dynamic select that includes the user's record
                dynamicSelectGroup.CreateDynamicSelect(
                    "SELECT id, name FROM user WHERE id=@userId", 
                    values: new {
                        userId = channel.id
                    },
                    name: "me"
                );

                // Build a dynamic list of chat ids for the user
                var chatIdsDynamicSelect = dynamicSelectGroup.CreateDynamicSelect(
                    "SELECT id, chat_id FROM chat_participant WHERE user_id=@userId", 
                    values: new {
                        userId = channel.id
                    }, 
                    name: "chat_ids"
                );
                var chatIds = chatIdsDynamicSelect.CreateMultiValueDynamicParam("chat_id");

                // Add a "chat" dynamic select that includes all the chats the user can see
                dynamicSelectGroup.CreateDynamicSelect(
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
                dynamicSelectGroup.CreateDynamicSelect(
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
                dynamicSelectGroup.CreateDynamicSelect(
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
                await dynamicSelectGroup.StartAsync();
                return dynamicSelectGroup;
            });

            // Listen for API requests to /api/profile/update
            webServer.OnPost("/api/profile/update", async (req, res) => {
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
            webServer.OnPost("/api/chat/create", async(req, res) => {
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
            webServer.OnPost("/api/chat/join", async (req, res) => {
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
            webServer.OnPost("/api/chat/delete", async (req, res) => {
                logger.Debug("Main():/api/chat/delete");
                var auth = req.AuthenticationHeaderValue;
                var chat = await req.ParseAsJsonAsync<dynamic>();

                // Create records in database
                await database.DeleteAndCommitAsync("DELETE FROM chat WHERE id=@chatId", new {
                    chatId = chat.id
                });
            });

            // Listen for API requests to /api/chat/message
            webServer.OnPost("/api/chat/message", async(req, res) => {
                var auth = req.AuthenticationHeaderValue;
                var chatMessage = await req.ParseAsJsonAsync<dynamic>();

                // Create record in database
                await database.InsertAndCommitAsync("INSERT INTO chat_message (@@names) VALUES (@@values)", new {
                    user_id = auth.Parameter,
                    chat_id = chatMessage.chatId,
                    text = chatMessage.text
                });
            });

            webServer.Start();
            ConsoleUtil.WaitForCancelKey();
            webServer.Stop();
        }

    }

}
