# Butterfly Framework
> Declarative framework for building real-time apps using C#

Create real-time apps with minimal coding.  Declare the data that should be synchronized with each client using familiar SELECT syntax. Let the Butterfly Framework send the data and any changes to each client.

## Getting Started

1. Clone the github repository `https://github.com/fireshark/butterfly-framework.git`
1. Open `Butterfly.sln` in Visual Studio 2017

## Hello World Example

Example C# code running on a server...

```csharp
// Create an EmbedIO web server to use as the implementation for our ChannelServer and WebApiServer
var embedIOWebServer = new Unosquare.Labs.EmbedIO.WebServer("http://localhost:8080/"));

// Create a channelServer allowing the server to push data to clients
var channelServer = new EmbedIOChannelServer(embedIOWebServer);

// Create a webApiServer (allows the server to receive RESTful API requests)
var webApiServer = new EmbedIOWebApiServer(embedIOWebServer);

// Create a database (allows doing INSERTs, UPDATEs, and DELETEs while receiving data change events)
var database = new MySqlDatabase("Server=127.0.0.1;Uid=test;Pwd=test!123;Database=test");

// Listen for clients creating new channels
// (clients are expected to create and maintain a single channel to the server)
channelServer.OnNewChannelAsync("/message", async(channel) => {
    // When a channel is created, create a dynamic view on the message table
    // and send all data events to the client over the channel
    return database.CreateAndStartDynamicView(
        "SELECT * FROM message",
        dataEventTransaction => {
            channel.Queue(dataEventTransaction);
        }
    );
});

// Listen for POST requests to /api/message
webApiServer.OnPost("/api/message", async(req, res) => {
    // Parse the received message
    var message = await req.ParseAsJsonAsync<dynamic>();

    // INSERT a record into the message table
    // (this will trigger the DynamicView above to send the INSERT event to the client over the channel)
    await database.InsertAndCommitAsync("message", new {
        text = message
    });
});

channelServer.Start();
webApiServer.Start();
```

Example javascript code running in a browser client...

```js
// This create a channel to the server (using a WebSocket)
// and echoes any data events received to the console
let channelClient = new WebSocketChannelClient({
    url: 'ws://localhost:8080/message?id=123',
    dataEventHandler: function(dataEventTransaction) {
        console.log(dataEventTransaction.dataEvents[0].text);
    } 
});
channelClient.start();
```

Running the server above would cause a "Hello World" record to be inserted into the database and replicated to a client that echoes this data event to the console.

In more complex scenarios, you would define all the DynamicViews that a given client should see and let the framework handle synchronizing the data to the client.  Web clients can easily be integrated with Vue.js, Angular, React, etc to automatically update the web UI as the data changes.

## Demos

- [Simple Chat (Using Vue.js)](http://examples.butterflyframework.io/examples/simple-chat/index.vue.html)

- [Simple Chat (Using Angular)](http://examples.butterflyframework.io/examples/simple-chat/index.angular.html)

- [Full Chat (Using Vue.js)](http://examples.butterflyframework.io/examples/full-chat/index.vue.html)

## Packages

There are three key packages to the Butterfly Framework...

- [Butterfly.Database](https://firesharkstudios.github.io/Butterfly/Butterfly.Database) - Allows executing INSERT, UPDATE, and DELETE statements; creating dynamic views; and receiving data change events both on tables and dynamic views.  This is the bread and butter of the Butterfly Framework.
 
- [Butterfly.Channel](https://firesharkstudios.github.io/Butterfly/Butterfly.Channel) - Allows clients to create new channels to the server and allows the server to push messages to connected clients.

- [Butterfly.WebApi](https://firesharkstudios.github.io/Butterfly/Butterfly.WebApi) - Allows receiving API requests via HTTP (inspired by Node.js' Express) by wrapping existing C# web servers.

There are no interdependencies between the packages. Use any subset of the packages you wish.

## Status

Still pre-alpha but actively implementing features and test cases.

## Contributing

If you'd like to contribute, please fork the repository and use a feature
branch. Pull requests are warmly welcome.

## Licensing

The core server code is licensed under the Apache License 2.0.  

The example server code is licensed under the MIT License.

All the client code is licensed under the MIT License.

