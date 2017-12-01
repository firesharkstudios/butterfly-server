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

// Create a channelServer (allows the server to push data to client)
var channelServer = new EmbedIOChannelServer(embedIOWebServer);

// Create a webApiServer (allows the server to receive RESTful API requests)
var webApiServer = new EmbedIOWebApiServer(embedIOWebServer);

// Create a database (allows doing INSERTs, UPDATEs, and DELETEs while receiving data change events)
var database = new MySqlDatabase("Server=127.0.0.1;Uid=test;Pwd=test!123;Database=test");

// Clients are expected to create and maintain a single channel to the server,
// listen for clients creating new channels
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

## Packages

There are three key packages to the Butterfly Framework...

- [Butterfly.Database](https://firesharkstudios.github.io/Butterfly/Butterfly.Database) - Server code that publishes data change events when executing INSERTs, UPDATEs, and DELETEs using the provided API.
 
- [Butterfly.Channel](https://firesharkstudios.github.io/Butterfly/Butterfly.Channel) - Client and server code that allow pushing data from the server to the client and updating local data structures.  Clients create a single channel to the server on startup.

- [Butterfly.WebApi](https://firesharkstudios.github.io/Butterfly/Butterfly.WebApi) - Server code that allow serving static files and responding to API requests via HTTP requests.

All the packages are independent with no interdependencies. Use all the packages together or use any subset of the packages you wish.

## Status

Still pre-alpha but actively implementing features and test cases.

## Contributing

If you'd like to contribute, please fork the repository and use a feature
branch. Pull requests are warmly welcome.

## Licensing

The core server code is licensed under the Apache License 2.0.  

The example server code is licensed under the MIT License.

All the client code is licensed under the MIT License.

