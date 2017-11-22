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
using System.IO;

using RedHttpServerNet45;

using Butterfly.Channel;
using Butterfly.Database;
using Butterfly.Util;
using Butterfly.WebServer;

namespace SimpleChatServer {
    class Program {
        static void Main(string[] args) {
            // Setup database
            Database database = new Butterfly.Database.Memory.MemoryDatabase();
            database.CreateFromTextAsync(@"CREATE TABLE chat_message (
	            id INT NOT NULL AUTO_INCREMENT,
	            user_name VARCHAR(40) NOT NULL,
	            text VARCHAR(255) NOT NULL,
	            created_at DATETIME NOT NULL,
	            PRIMARY KEY (id)
            );").Wait();
            database.SetDefaultValue("created_at", () => DateTime.Now);

            // Setup web server and channel manager
            int port = 8080;
            RedHttpServer redHttpServer = new RedHttpServer(port, Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, @"..\..\..\Butterfly.WebClient")));
            BaseWebServer webServer = new Butterfly.RedHttpServer.RedHttpServerWebServer(redHttpServer);
            BaseChannelServer channelServer = new Butterfly.RedHttpServer.RedHttpServerChannelServer(redHttpServer);

            // Send initial data events and subsequent data events to any new channels created
            channelServer.OnNewChannel(channel => {
                var initialDataEventTransaction = database.GetInitialDataEventTransactionAsync("chat_message").Result;
                channel.Queue(initialDataEventTransaction);
                return database.OnNewCommittedTransaction(dataEventTransaction => {
                    channel.Queue(dataEventTransaction);
                });
            });

            // Listen for API requests to /api/chat/message
            webServer.OnPost("/api/chat/message", async(req, res) => {
                var chatMessage = await req.ParseAsJsonAsync<dynamic>();
                await database.InsertAndCommitAsync("chat_message", new {
                    user_name = chatMessage.userName,
                    text = chatMessage.text
                });
            });

            // Start web server (and stop web server if cancel key pressed)
            webServer.Start();
            Console.WriteLine($"Open http://localhost:{port}/examples/simple-chat/index.html in different browsers (or under different personas in Chrome)");
            ConsoleUtil.WaitForCancelKey();
            webServer.Stop();
        }
    }
}