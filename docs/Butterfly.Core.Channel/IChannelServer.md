# IChannelServer interface

Allows clients to create new channels to the server and allows the server to push messages to connected clients.

Allows clients to create new channels to the server and allows the server to push messages to connected clients.

Allows clients to create new channels to the server and allows the server to push messages to connected clients.

Allows clients to create new channels to the server and allows the server to push messages to connected clients.

Allows clients to create new channels to the server and allows the server to push messages to connected clients.

Allows clients to create new channels to the server and allows the server to push messages to connected clients.

Allows clients to create new channels to the server and allows the server to push messages to connected clients.

Allows clients to create new channels to the server and allows the server to push messages to connected clients.

Allows clients to subscribe to channels and allows the server to push data to subscribed clients.

Allows clients to subscribe to channels and allows the server to push data to subscribed clients.

```csharp
public interface IChannelServer : IDisposable
```

## Members

| name | description |
| --- | --- |
| [AuthenticatedConnections](IChannelServer/AuthenticatedConnections.md) { get; } |  |
| [UnauthenticatedConnections](IChannelServer/UnauthenticatedConnections.md) { get; } | Number of connections |
| [GetConnection](IChannelServer/GetConnection.md)(…) | Retrieve a channel by id |
| [OnSubscribe](IChannelServer/OnSubscribe.md)(…) |  |
| [Start](IChannelServer/Start.md)() | Starts the channel server |

## Remarks

Initialize a channel server instance with a single route and single default channel...

```csharp
var channelServer = new SomeChannelServer();
var route = channelServer.RegisterRoute("/chat");
route.RegisterChannel(handlerAsync: async(vars, channel) => {
    // Do stuff here to initialize the channel (send initial data, listen for specific data change events, etc)
    // and return any object that should be disposed when the channel is disposed
});
channelServer.Start();

ster a default channel that creates a DynamicView on the chat_message table sending all data to the channel
```

If a client has now created a channel at /chat?id=123, the server can now push data to the client via...

```csharp
channelServer.Queue("123", "Hello");
```

If you no longer need a channel server instance, call Dispose() on the channel server...

```csharp
channelServer.Dispose();
```

Initialize a channel server instance with a single route and single default channel...

```csharp
var channelServer = new SomeChannelServer();
var route = channelServer.RegisterRoute("/chat");
route.RegisterChannel(handlerAsync: async(vars, channel) => {
    // Do stuff here to initialize the channel (send initial data, listen for specific data change events, etc)
    // and return any object that should be disposed when the channel is disposed
});
channelServer.Start();

ster a default channel that creates a DynamicView on the chat_message table sending all data to the channel
```

If a client has now created a channel at /chat?id=123, the server can now push data to the client via...

```csharp
channelServer.Queue("123", "Hello");
```

If you no longer need a channel server instance, call Dispose() on the channel server...

```csharp
channelServer.Dispose();
```

Initialize a channel server instance with a single route and single default channel...

```csharp
var channelServer = new SomeChannelServer();
var route = channelServer.RegisterRoute("/chat");
route.RegisterChannel(handlerAsync: async(vars, channel) => {
    // Do stuff here to initialize the channel (send initial data, listen for specific data change events, etc)
    // and return any object that should be disposed when the channel is disposed
});
channelServer.Start();

ster a default channel that creates a DynamicView on the chat_message table sending all data to the channel
```

If a client has now created a channel at /chat?id=123, the server can now push data to the client via...

```csharp
channelServer.Queue("123", "Hello");
```

If you no longer need a channel server instance, call Dispose() on the channel server...

```csharp
channelServer.Dispose();
```

Initialize a channel server instance with a single route and single default channel...

```csharp
var channelServer = new SomeChannelServer();
var route = channelServer.RegisterRoute("/chat");
route.RegisterChannel(handlerAsync: async(vars, channel) => {
    // Do stuff here to initialize the channel (send initial data, listen for specific data change events, etc)
    // and return any object that should be disposed when the channel is disposed
});
channelServer.Start();

ster a default channel that creates a DynamicView on the chat_message table sending all data to the channel
```

If a client has now created a channel at /chat?id=123, the server can now push data to the client via...

```csharp
channelServer.Queue("123", "Hello");
```

If you no longer need a channel server instance, call Dispose() on the channel server...

```csharp
channelServer.Dispose();
```

Initialize a channel server instance with a single route and single default channel...

```csharp
var channelServer = new SomeChannelServer();
var route = channelServer.RegisterRoute("/chat");
route.RegisterChannel(handlerAsync: async(vars, channel) => {
    // Do stuff here to initialize the channel (send initial data, listen for specific data change events, etc)
    // and return any object that should be disposed when the channel is disposed
});
channelServer.Start();

ster a default channel that creates a DynamicView on the chat_message table sending all data to the channel
```

If a client has now created a channel at /chat?id=123, the server can now push data to the client via...

```csharp
channelServer.Queue("123", "Hello");
```

If you no longer need a channel server instance, call Dispose() on the channel server...

```csharp
channelServer.Dispose();
```

Initialize a channel server instance with a single route and single default channel...

```csharp
var channelServer = new SomeChannelServer();
var route = channelServer.RegisterRoute("/chat");
route.RegisterChannel(handlerAsync: async(vars, channel) => {
    // Do stuff here to initialize the channel (send initial data, listen for specific data change events, etc)
    // and return any object that should be disposed when the channel is disposed
});
channelServer.Start();

ster a default channel that creates a DynamicView on the chat_message table sending all data to the channel
```

If a client has now created a channel at /chat?id=123, the server can now push data to the client via...

```csharp
channelServer.Queue("123", "Hello");
```

If you no longer need a channel server instance, call Dispose() on the channel server...

```csharp
channelServer.Dispose();
```

Initialize a channel server instance with a single route and single default channel...

```csharp
var channelServer = new SomeChannelServer();
var route = channelServer.RegisterRoute("/chat");
route.RegisterChannel(handlerAsync: async(vars, channel) => {
    // Do stuff here to initialize the channel (send initial data, listen for specific data change events, etc)
    // and return any object that should be disposed when the channel is disposed
});
channelServer.Start();

ster a default channel that creates a DynamicView on the chat_message table sending all data to the channel
```

If a client has now created a channel at /chat?id=123, the server can now push data to the client via...

```csharp
channelServer.Queue("123", "Hello");
```

If you no longer need a channel server instance, call Dispose() on the channel server...

```csharp
channelServer.Dispose();
```

Initialize a channel server instance with a single route and single default channel...

```csharp
var channelServer = new SomeChannelServer();
var route = channelServer.RegisterRoute("/chat");
route.RegisterChannel(handlerAsync: async(vars, channel) => {
    // Do stuff here to initialize the channel (send initial data, listen for specific data change events, etc)
    // and return any object that should be disposed when the channel is disposed
});
channelServer.Start();

ster a default channel that creates a DynamicView on the chat_message table sending all data to the channel
```

If a client has now created a channel at /chat?id=123, the server can now push data to the client via...

```csharp
channelServer.Queue("123", "Hello");
```

If you no longer need a channel server instance, call Dispose() on the channel server...

```csharp
channelServer.Dispose();
```

Listen for subscription requests to the todos channel...

```csharp
var channelServer = new SomeChannelServer();
// Listen for subscribe requests...
// - The handler must return an IDisposable object (gets disposed when the channel is unsubscribed)
// - The handler can push data to the client by calling channel.Queue()
channelServer.OnSubscribe("todos", (vars, channel) => {
    return database.CreateAndStartDynamicView("todo", dataEventTransaction => channel.Queue(dataEventTransaction));
});
channelServer.Start();
```

Listen for subscription requests to the todos channel...

```csharp
var channelServer = new SomeChannelServer();
// Listen for subscribe requests...
// - The handler must return an IDisposable object (gets disposed when the channel is unsubscribed)
// - The handler can push data to the client by calling channel.Queue()
channelServer.OnSubscribe("todos", (vars, channel) => {
    return database.CreateAndStartDynamicView("todo", dataEventTransaction => channel.Queue(dataEventTransaction));
});
channelServer.Start();
```

## See Also

* namespace [Butterfly.Core.Channel](../Butterfly.Core.md)

<!-- DO NOT EDIT: generated by xmldocmd for Butterfly.Core.dll -->
