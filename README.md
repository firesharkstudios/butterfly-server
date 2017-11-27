# Butterfly Framework
> Declarative framework for building real-time apps using C#

Create real-time apps with minimal coding.  Declare the data that should be synchronized with each client using familiar SELECT syntax. Let the Butterfly Framework send the data and any changes to each client.

## Getting Started

1. Clone the github repository `https://github.com/fireshark/butterfly-framework.git`
1. Open `Butterfly.sln` in Visual Studio 2017

## Overview

There are three key packages to the Butterfly Framework...

- `Butterfly.Database` - Server code that publishes data change events when executing INSERTs, UPDATEs, and DELETEs using the provided API.
 
- `Butterfly.Channel` - Client and server code that allow pushing data from the server to the client and updating local data structures.  Clients create a single channel to the server on startup.

- `Butterfly.Web` - Server code that allow serving static files and responding to API requests via HTTP requests.

All the packages are independent with no interdependencies. Use all the packages together or use any subset of the packages you wish.

## Butterfly.Database Hello World

This C# code runs on a server...

```csharp
// Create a DynamicSelectGroup that print any changes in child DynamiSelects to the console
var dynamicSelectGroup = database.CreateDynamicSelectGroup(listener: dataEventTransaction => {
    Console.WriteLine(dataEventTransaction);
});

// Create a child DynamicSelect that sends the initial rows and any changes to the rows to the parent DynamicSelectGroup
dynamicSelectGroup.CreateDynamicSelect("SELECT * FROM chat_message WHERE user_id=@userId", values: new {
    userId = 123
});

// Add a record that would change the rows in the SELECT of the DynamicSelect above
await database.InsertAndCommitAsync("chat_message", values: {
    user_id: 123_
    text: "Hello World"
})
```

The above code...
- Creates a `DynamicSelectGroup` that echoes any data event transactions to the Console
- Creates a child `DynamicSelect` on the `DynamicSelectGroup` that retrieves the initial data _and_ listens for data changes
- Executes an INSERT to trigger a data change on the `DynamicSelect`

## Butterfly.Channel Hello World

This C# code runs on a server...
```csharp
// Initialize new channels created
channelServer.OnNewChannelAsync(async(channel) => {
    channel.Queue("Hello World");
});
```

The above code listens for a new channel to be created by a client and pushes "Hello World" to the client when a channel is created.

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

## Putting It All Together Hello World

The example below combines all three packages to create a server in C# that synchronizes the `chat_message` table on a server with clients and listens for POST requests to `/api/chat/message` to insert new records in the `chat_message` table.

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

// Listen for API requests to /api/chat/message
webServer.OnPost("/api/chat/message", async(req, res) => {
    var chatMessage = await req.ParseAsJsonAsync<dynamic>();
    await database.InsertAndCommitAsync("chat_message", new {
        user_name = chatMessage.userName,
        text = chatMessage.text
    });
});
```

The first chunk of code above...
- Listens for a channel to be created by a client (via `channelServer.OnNewChannelAsync`)
- Creates a `DynamicSelectGroup` that sends any data event transactions to the client (via `channel.Queue`)
- Creates a `DynamicSelect` that defines the data the client should receive initially and when any of this data changes

The second chunk of code above listens for POST requests to `/api/chat/message` and INSERTs a new record in the `chat_message` table.

This javascript runs on a web client...

```js
let channelClient = new WebSocketChannelClient({
    channelId: '123',
    dataEventHandler: new ArrayDataEventHandler({
        arrayMapping: {
            chat_message: chatMessages,
        }
    }),
});
channelClient.start();
```

The code above uses `WebSocketChannelClient` to create a channel (WebSocket) to the server and registers an instance of `ArrayDataEventHandler` to handle the data pushed by the server mapping any changes in the `chat_message` table to the local `chatMessages` array.

## Contributing

If you'd like to contribute, please fork the repository and use a feature
branch. Pull requests are warmly welcome.

## Licensing

Everything is licensed under the Apache License 2.0.

