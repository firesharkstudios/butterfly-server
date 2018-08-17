# Butterfly Server .NET ![Butterfly Logo](https://raw.githubusercontent.com/firesharkstudios/butterfly-server-dotnet/master/img/logo-40x40.png) 

> The Everything is Real-Time C# Backend for Single Page Applications

Key goals...

- Define server datasets in familiar SELECT syntax
- Auto sync server datasets to clients over a WebSocket
- Define a RESTlike API using an Express like syntax
- Work with any client framework (Vue, React, Angular, etc)

## Todo List Example

### Run It Yourself

1. Clone https://github.com/firesharkstudios/butterfly-server-dotnet.git
1. In a terminal or command prompt...
   - Change to the `Butterfly.Example.Todo.Client` directory
   - Run `npm install`
1. In Visual Studio 2017...
   - Open `Butterfly.sln`
   - Run `Butterfly.Example.Todo.Server`
1. In a terminal or command prompt...
   - Change to the `Butterfly.Example.Todo.Client` directory
   - Run `npm run dev`

You should see http://localhost:8080/ open in a browser. Try opening a second browser instance at http://localhost:8080/. Notice that changes are automatically synchronized between the two browser instances.

### Understanding the Server

Here is the key server code for our todo list manager...

```csharp
public static void Init(IDatabase database, IWebApiServer webApiServer, IChannelServer channelServer) {
    // Listen for API requests
    webApiServer.OnPost($"/api/todo/insert", async (req, res) => {
	var todo = await req.ParseAsJsonAsync<Dict>();
	await database.InsertAndCommitAsync<string>("todo", todo);
    });
    webApiServer.OnPost($"/api/todo/delete", async (req, res) => {
	var id = await req.ParseAsJsonAsync<string>();
	await database.DeleteAndCommitAsync("todo", id);
    });

    // Listen for websocket connections to /ws
    var route = channelServer.RegisterRoute("/ws");

    // Register a channel that creates a DynamicView on the todo table 
    // (sends all the initial data in the todo table and sends changes to the todo table)
    route.RegisterChannel(
	channelKey: "todos", 
	handlerAsync: async (vars, channel) => await database.CreateAndStartDynamicView(
	    "todo",
	    listener: dataEventTransaction => channel.Queue(dataEventTransaction)
	)
    );
}
```

The above C# code...
- Defines a simple API to insert and delete *todo* records
- Listens for WebSocket connections at */ws*
- Allows clients to subscribe to a *todos* channel (clients receive both the initial *todo* records **and** any changes to the *todo* records)

See [Butterfly.Example.Todo.Server](https://github.com/firesharkstudios/butterfly-server-dotnet/tree/master/Butterfly.Example.Todo.Server) for the working server code.

### Understanding the Client

Now, let's see how a client might interact with this server using the Butterfly Client (`npm install butterfly-client`).

First, the client should maintain an open WebSocket to the server by using the *WebSocketChannelClient* class...

```js
let channelClient = new WebSocketChannelClient({
    url: `ws://${window.location.host}/ws`
});
channelClient.start('Custom My-User-Id');
```

Next, the client will want to subscribe to a channel to receive data...

```js
let todosList = [];
channelClient.subscribe(
    new ArrayDataEventHandler({
        arrayMapping: {
            todo: todosList
        }
    }),
    'todos'
);
```

This subscription will cause the local *todosList* array to be synchronized with the *todo* records on the server.

Next, let's invoke a method on our API to add a new *todo* record (use whatever client HTTP library you wish)...

```js
$.ajax('/api/todo/insert', {
  method: 'POST',
  data: JSON.stringify({
    name: 'My First To-Do',
  }),
});
```

After the above code runs, the server will have a new *todo* record and a new *todo* record will automagically be sychronized from the server to the client's local *todosList* array.

See [Butterfly.Example.Todo.Client](https://github.com/firesharkstudios/butterfly-server-dotnet/tree/master/Butterfly.Example.Todo.Client) for a full working client based on Vuetify and Vue.

## More Complex Subscriptions

In the Todo Manager example above, we subscribed to all the data in a single *todo* table; however, much more complex subscriptions are supported...

```cs
channelRoute.RegisterChannel("todo-page", handlerAsync: async(vars, channel) => {
  var dynamicViewSet = database.CreateDynamicViewSet(dataEventTransaction => channel.Queue(dataEventTransaction);

  string userId = channel.Connection.AuthToken;

  // DynamicViews can include JOINs and will update if 
  // any of the joined tables change the resultset
  // (note this requires using a database like MySQL that supports JOINs)
  dynamicViewSet.CreateDynamicView(
    @"SELECT td.id, td.name, td.user_id, u.name user_name
    FROM todo td
      INNER JOIN user u ON td.user_id=u.id
    WHERE u.id=@userId",
    new {
      userId
    }
  );

  // A channel can return multiple resultsets as well
  dynamicViewSet.CreateDynamicView(
    @"SELECT id, name
    FROM tag
    WHERE user_id=@userId",
    new {
      userId
    }
  );

  return dynamicViewSet;
);
```

In this example, a client subscribing to *todo-page* will get a *todo* collection and a *tag* collection both filtered by user id.  

Because the new *todo* collection is the result of a join, the client will receive updates if changes to either of the underlying *todo* table or *user* table would change the resultset.


## Getting Started

You can either install the binaries from NuGet...

```
nuget install Butterfly.Core

# If you wish to use EmbedIO as a ChannelServer and WebApiServer...
nuget install Butterfly.EmbedIO

# If you wish to use MySQL as your database...
nuget install Butterfly.MySQL

# If you wish to use AWS SES to send emails...
nuget install Butterfly.Aws

# If you wish to use Twilio to send text messages...
nuget install Butterfly.Twilio
```

Or you can get the source from GitHub...

1. Clone the github repository `https://github.com/firesharkstudios/butterfly-server-dotnet.git`
1. Open `Butterfly.sln` in Visual Studio 2017
1. Run the appropriate example project

## Packages

Here are the key packages in *Butterfly.Core*...

- [Butterfly.Core.Auth](https://github.com/firesharkstudios/butterfly-server-dotnet/tree/master/docs/Butterfly.Core.Auth) - Allows registering and logging in users, handling forgot password and reset password requests, and validating auth tokens.
- [Butterfly.Core.Channel](https://github.com/firesharkstudios/butterfly-server-dotnet/tree/master/docs/Butterfly.Core.Channel) - Allows clients to create new channels to the server and allows the server to push messages to connected clients (think WebSockets).
- [Butterfly.Core.Database](https://github.com/firesharkstudios/butterfly-server-dotnet/tree/master/docs/Butterfly.Core.Database) - Allows executing SELECT statements, creating transactions to execute INSERT, UPDATE, and DELETE statements; creating dynamic views; and receiving data change events both on tables and dynamic views.  This is the bread and butter of the Butterfly Server .NET.
- [Butterfly.Core.Notify](https://github.com/firesharkstudios/butterfly-server-dotnet/tree/master/docs/Butterfly.Core.Notify) - Allows sending notifications (email/texts) to users.
- [Butterfly.Core.WebApi](https://github.com/firesharkstudios/butterfly-server-dotnet/tree/master/docs/Butterfly.Core.WebApi) - Allows receiving API requests via HTTP (inspired by Express JS) by wrapping existing C# web servers.

Here are various implementations you'll likely find useful...

- [Butterfly.Aws](https://github.com/firesharkstudios/butterfly-server-dotnet/tree/master/docs/Butterfly.Aws) - Implementation of *Butterfly.Core.Notify* for AWS SES
- [Butterfly.MySql](https://github.com/firesharkstudios/butterfly-server-dotnet/tree/master/docs/Butterfly.MySql) - Implementation of *Butterfly.Core.Database* for MySql
- [Butterfly.EmbedIO](https://github.com/firesharkstudios/butterfly-server-dotnet/tree/master/docs/Butterfly.EmbedIO) - Implementation of *Butterfly.Core.Channel* and *Butterfly.Core.WebApi* for [EmbedIO](https://github.com/unosquare/embedio) server
- [Butterfly.Twilio](https://github.com/firesharkstudios/butterfly-server-dotnet/tree/master/docs/Butterfly.Twilio) - Implementation of *Butterfly.Notify* for Twilio SMS

## In the Wild

[Build Hero](https://www.buildhero.io) is a collaborative tool for general contractors, subcontractors, and customers to collaborate on remodel projects.  The [my.buildhero.io](https://my.buildhero.io) site, the Android app, and the iOS app are all powered by Butterfly Server .NET.

## Contributing

If you'd like to contribute, please fork the repository and use a feature
branch. Pull requests are warmly welcome.

## Licensing

The code is licensed under the Apache License 2.0.