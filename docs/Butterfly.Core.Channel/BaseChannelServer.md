# BaseChannelServer class

Allows clients to create new channels to the server and allows the server to push messages to connected clients.

Allows clients to create new channels to the server and allows the server to push messages to connected clients.

Allows clients to create new channels to the server and allows the server to push messages to connected clients.

Allows clients to create new channels to the server and allows the server to push messages to connected clients.

Allows clients to create new channels to the server and allows the server to push messages to connected clients.

Allows clients to create new channels to the server and allows the server to push messages to connected clients.

Allows clients to create new channels to the server and allows the server to push messages to connected clients.

```csharp
public abstract class BaseChannelServer : IChannelServer
```

## Public Members

| name | description |
| --- | --- |
| [BaseChannelServer](BaseChannelServer/BaseChannelServer.md)(…) |  |
| [AuthenticatedConnections](BaseChannelServer/AuthenticatedConnections.md) { get; } |  |
| [ChannelSubscriptionByKey](BaseChannelServer/ChannelSubscriptionByKey.md) { get; } |  |
| [UnauthenticatedConnections](BaseChannelServer/UnauthenticatedConnections.md) { get; } | Number of connections |
| readonly [getAuthToken](BaseChannelServer/getAuthToken.md) |  |
| readonly [getAuthTokenAsync](BaseChannelServer/getAuthTokenAsync.md) |  |
| readonly [getId](BaseChannelServer/getId.md) |  |
| readonly [getIdAsync](BaseChannelServer/getIdAsync.md) |  |
| [AddUnauthenticatedConnection](BaseChannelServer/AddUnauthenticatedConnection.md)(…) |  |
| [Dispose](BaseChannelServer/Dispose.md)() |  |
| [GetConnection](BaseChannelServer/GetConnection.md)(…) | Retrieve a channel by id |
| [OnSubscribe](BaseChannelServer/OnSubscribe.md)(…) |  |
| [Start](BaseChannelServer/Start.md)() | Starts the channel server |

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

## See Also

* interface [IChannelServer](IChannelServer.md)
* namespace [Butterfly.Core.Channel](../Butterfly.Core.md)

<!-- DO NOT EDIT: generated by xmldocmd for Butterfly.Core.dll -->
