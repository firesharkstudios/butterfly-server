# Butterfly Framework
> Declarative framework for building real-time apps using C#

Create real-time apps with minimal coding.  Declare the data that should be synchronized with each client using familiar SELECT syntax. Let the Butterfly Framework send the data and any changes to each client.

## Getting Started

1. Clone the github repository `https://github.com/fireshark/butterfly-framework.git`
1. Open `Butterfly.sln` in Visual Studio 2017

## Overview

There are three key packages to the Butterfly Framework...

- `Butterfly.Database` - Server components that allow publishing change events when data changes.  Requires using `Butterfly.Database` to do INSERTs, UPDATEs, and DELETEs on the underlying database.
 
- `Butterfly.Channel` - Client and server components that allow pushing data from the server to the client and updating local data structures.  Clients create a single channel to the server on startup.

- `Butterfly.Web` - Server components that allow serving static files and responding to API requests via HTTP requests.

All the packages are independent with no interdependencies. Use all the packages together or use any subset of the packages you wish.

## Butterfly.Database Hello World

This C# code runs on a server...

```csharp
// Create a DynamicSelectGroup that print any changes in child DynamiSelects to the console
var dynamicSelectGroup = database.CreateDynamicSelectGroup(listener: dataEventTransaction => {
    Console.WriteLine(dataEventTransaction);
});

// Create a child DynamicSelect that sends the initial rows and any changes to the rows to the parent DynamicSelectGroup
dynamicSelectGroup.CreateDynamicSelect("SELECT * FROM chat_message WHERE user_id=@userId", values: {
    userId = 123
});

// Add a record that would change the rows in the SELECT of the DynamicSelect above
database.InsertAndCommitAsync("chat_message", values: {
    user_id: 123_
    text: "Hello World"
})
```

The above code...
- Creates a `DynamicSelectGroup` that echoes any data event transactions to the Console
- Creates a `DynamicSelect` to retrieve the initial data _and_ to listen for data changes
- Executes an INSERT to trigger a data change on the `DynamicSelect`

## Butterfly.Channel Hello World

This C# code runs on a server...
```csharp
// Initialize new channels created
channelServer.OnNewChannelAsync(async(channel) => {
    channel.Queue(new DataEventTransaction("Hello World"));
});
```

The above code listens for a new channel to be created by a client and pushes a "Hello World" data event transaction to the client when a channel is created.

This javascript code runs on a web client...
```js
let channelClient = new WebSocketChannelClient({
    channelId: '123',
    dataEventHandler: new EchoDataEventHandler(),
});
channelClient.start();
```
The above code creates a new channel to the server using an `EchoDataEventHandler` to print received data event transactions to the javascript console.

## Butterfly.WebServer Hello World

The following C# code runs on a server to listen for a GET request to `/test` and return "Hello World" to the requesting client...
```csharp
webServer.OnGet("/test", async (req, res) => {
    return "Hello World";
});
```

## Putting It All Together

This C# code runs on a server...

```csharp
// Initialize new channels created
channelServer.OnNewChannelAsync(async(channel) => {
    // Create a dynamic select group that sends changes to the channel
    var dynamicSelectGroup = database.CreateDynamicSelectGroup(listener: dataEventTransaction => {
        channel.Queue(dataChangeTransaction);
    });

    // Create a dynamic select that sends the initial rows and any changes to the row
    dynamicSelectGroup.CreateDynamicSelect(
        @"SELECT cm.id, cm.chat_id, cm.text, cm.created_at, u.id user_id, u.name 
            FROM chat_message cm INNER JOIN user u ON cm.user_id=u.id",
        name: "chat_message",
        keyFieldNames: new string[] { "id" }
    );

    return await dynamicSelectGroup.StartAsync();
});
```

The above code...
- Listens for a channel to be created by a client (`channelServer.OnNewChannelAsync`)
- Creates a `DynamicSelectGroup` that sends any data event transactions to the client (via `channel.Queue`)
- Creates a `DynamicSelect` that defines the data the client should receive initially and when any of this data changes

This javascript runs on a web client...

```js
let channelClient = new WebSocketChannelClient({
    channelId: '123',
    dataEventHandler: new VueDataEventHandler({
        vueArrayMapping: {
            chat_message: chatMessages,
        }
    }),
});
channelClient.start();
```

The code above uses `WebSocketChannelClient` to create a channel (WebSocket) to the server and registers an instance of `VueDataEventHandler` to handle the data pushed by the server mapping any changes in the `chat_message` table to the local `chatMessages` array.

## Goals

Support the following javascript data binding libraries...
- Vue.js
- Angular
- React 

Support the following databases...
- MySQL
- Postgres
- SQLite
- MS SQL Server
- MongoDB

Support the following web servers...
- Kestral
- Nancy FX
- Red HTTP Server

Support the following operating systems...
- Windows
- Linux
- Mac
- Raspian

Support the following run-times...
- .NET Framework
- .NET Core
- Mono

## Contributing

If you'd like to contribute, please fork the repository and use a feature
branch. Pull requests are warmly welcome.

## Licensing

Everything is licensed under the Apache License 2.0.

