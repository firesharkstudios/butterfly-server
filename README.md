![Logo of the project](https://raw.githubusercontent.com/jehna/readme-best-practices/master/sample-logo.png)

# Butterfly Framework
> Declarative framework for building real-time apps using C#

Create real-time web apps with minimal coding.  Declare the data that should be synchronized with each client using familiar SELECT syntax. Let the Butterfly Framework send the initial data and any changes to every client.

## Recommended App Architecture
Client side...

- Static HTML, CSS, and Javascript files
- Use `butterfly.channel.js` to receive data from the server (WebSockets, long polling, etc)
- Use `butterfly.data.js` to map the data to your favorite data binding library (Vue, Angular, React, etc)
- Call your REST API to make data changes and to perform actions

Server side...

- Use `Butterfly.WebServer` and `Butterfly.Channel` namespaces to use your favorite HTTP server (IIS, Kestral, Nancy FX, Red Web Server, etc)
- Use `Butterfly.Database` namespace to use your favorite database (MySQL, Postgres, SQLite, etc) 
- Implement your REST API using `webServer.OnGet`, `webServer.OnPost`, etc.
- Send data to clients using `channelServer.Send`
- Listen for database changes using `database.OnNewCommittedTransaction`

## Components
Client side...

- butterfly.channel.js
- butterfly.data.js

Server side...

- Butterfly.Channel
- Butterfly.WebServer
- Butterfly.Database
- Fireshark.Butterfly.Dynamic

## Getting Started

1. Clone the github repository `https://github.com/fireshark/butterfly-framework.git`
1. Open `Butterfly.sln` in Visual Studio 2017

## Simple Chat Example Walk Thru

This example shows a simple web page with the messages automatically synchronized across browsers.

### Try It

1. In Visual Studio 2017, run the `Butterfly.Examples.SimpleChatServer` project
1. Simulate two persons in a chat by opening `http://localhost:8080/examples/simple-chat/index.html` in two different browsers 
1. Type and send chat messages to see the chat history properly synchronized between the two browsers

## Break Down the Server Code

All the server code for this example is located in the `Main()` method of `Program.cs` in the `Butterfly.Examples.SimpleChatServer` project.  

Let's review each chunk of code in the `Main()` method.

The first chunk in the `Main()` method creates a `MemoryDatabase` with a single `chat_message` table defined...

```csharp
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
```

The `MemoryDatabase` above could be replaced with your favorite database implementation (MySQL, Postgres, SQLite, etc).  

To use an existing database structure instead of creating a new database structure, replace the `CreateDatabaseFromTextAsync` call above with a `ReadSchemaAsync` call.

The next chunk in the `Main()` method initializes a web server and channel server wrapping the `RedHttpServer`.

```csharp
// Setup web server and channel manager
int port = 8080;
RedHttpServer redHttpServer = new RedHttpServer(port, Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, @"..\..\..\Butterfly.WebClient")));
BaseWebServer webServer = new Butterfly.RedHttpServer.RedHttpServerWebServer(redHttpServer);
BaseChannelServer channelServer = new Butterfly.RedHttpServer.RedHttpServerChannelServer(redHttpServer);
```

The `webServer` is used to serve static files and to receive API requests.  The `RedHttpServerWebServer` could be replaced with a different implementation for your favorite web server.

The `channelServer` is used to push data to the client. The `RedHttpServerChannelServer` could be replaced with a different implementation for your favorite push technique (WebSocket, long polling, etc).

The next chunk in the `Main()` method registers an `OnNewChannel` listener that in turn registers a `OnNewCommittedTransactionListner` which sends database changes to the `channel` as appropriate...

```csharp
// Send initial data events and subsequent data events to any new channels created
channelServer.OnNewChannel(channel => {
    var initialDataEventTransaction = database.GetInitialDataEventTransactionAsync("chat_message").Result;
    channel.Queue(initialDataEventTransaction);
    return database.OnNewCommittedTransaction(dataEventTransaction => {
        channel.Queue(dataEventTransaction);
    });
});
```

The next chunk in the `Main()` method INSERTs a new record in the `chat_message` table when a POST request is received at `/api/chat/message`...

```csharp
// Listen for API requests to /api/chat/message
webServer.OnPost("/api/chat/message", async(req, res) => {
    var chatMessage = await req.ParseAsJsonAsync<dynamic>();
    await database.InsertAndCommitAsync("chat_message", new {
        user_name = chatMessage.userName,
        text = chatMessage.text
    });
});
```

The final chunk in the `Main()` method starts the web server, waits for the escape key to be pressed, and stops the web server...

```csharp
// Start web server (and stop web server if cancel key pressed)
webServer.Start();
Console.WriteLine($"Open http://localhost:{port}/examples/simple-chat/index.html in different browsers (or under different personas in Chrome)");
ConsoleUtil.WaitForCancelKey();
webServer.Stop();
```

## Break Down the Client Code

All the client code for this example is located in the `index.js` in the `Butterfly.WebClient` project in the `examples/simple-chat/index.js` subdirectory.  

This specific example uses Vue.js; however, an equivalent client could be created using Angular or React.

Let's review the following chunk of code in `index.js` that handles receiving pushed data from the server and mapping it to local data structures...

```js
// Create channel to server and handle data events
let channelClient = new WebSocketChannelClient({
    userId: self.myUserId,
    dataEventHandler: new VueDataEventHandler({
        vueArrayMapping: {
            chat_message: self.chatMessages,
        }
    }),
    onStatusChange: function (value) {
        self.connectionStatus = value;
    },
});
channelClient.start();
```

The `WebSocketChannelClient` class creates a connection to the server that maps data pushed by the server to the registered `dataEventHandler`.

When the registered `VueDataEventHandler` instance receives new data, it maps the changes to a local data structure (mapping changes in table `chat_message` to array `self.chatMessages` in this specific case).  The magic of Vue.js then allows these changes to automatically rerender the HTML to display the changes immediately.

## Contributing

If you'd like to contribute, please fork the repository and use a feature
branch. Pull requests are warmly welcome.

## Licensing

Everything is licensed under the Apache License 2.0.

