# Butterfly Framework ![Butterfly Logo](https://raw.githubusercontent.com/firesharkstudios/Butterfly/master/img/logo-40x40.png) 

> The Everything is Real-Time C# Backend for Single Page Applications

Create your Single Page Application in your favorite framework ([Vue.js](https://vuejs.org/), [AngularJS](https://angularjs.org/), [React](https://reactjs.org/)) and use the Butterfly Framework to build your server.

Key goals of the Butterfly Framework...

- Easily define a RESTful API
- Auto sync data with clients

## Overview

Let's see how the Butterly Framework would help us build a simple to-do list manager.

### The Server

First, let's use the *WebApiServer* to define our API...

```csharp
webApiServer.OnPost($"/api/to-do/insert", async(req, res) => {
  var todo = await req.ParseAsJsonAsync<dynamic>();
  await database.InsertAndCommitAsync<string>("todo", todo);
});

webApiServer.OnPost($"/api/to-do/update", async(req, res) => {
  var todo = await req.ParseAsJsonAsync<dynamic>();
  await database.UpdateAndCommitAsync<string>("todo", todo);
});

webApiServer.OnPost($"/api/to-do/delete", async(req, res) => {
  var id = await req.ParseAsJsonAsync<string>();
  await database.DeleteAndCommitAsync<string>("todo", id);
});
```

Clients would call the above API to insert, update, and delete records in the *todo* table.

Next, let's use the *ChannelServer* to listen for web socket requests to */listen*...

```cs
var channelRoute = channelServer.RegisterRoute("/listen");
```

Clients would maintain an open web socket to */listen* to receive data from the server.

Finally, let's allow clients to subscribe to a *todo-page* channel...

```cs
channelRoute.RegisterChannel("todo-page", handlerAsync: async(vars, channel) => {
  return await database.CreateAndStartDynamicView(
    sql: "todo",
    listener: dataEventTransaction => {
      channel.Queue("DATA-EVENT-TRANSACTION", dataEventTransaction);
    }
  );
);
```

Upon first subscribing, clients will receive all the records in the *todo* table.  If any of this data changes, clients will also receive changes to this data.

### The Client

Now, let's see how a client might interact with this server using the Butterfly Client (`npm install butterfly-client`).

First, let's create a *WebSocketChannelClient* that connects to */listen*, subscribes to the *todo-page* channel, and maps *todo* data events to the local *todoItems* array....

```js
let myUserId = '123';
let todoItems = [];
let channelClient = new WebSocketChannelClient({
  url: '/listen',
});
channelClient.subscribe(new ArrayDataEventHandler({
  arrayMapping: {
    todo: todoItems,
  }
}), 'todo-page');
channelClient.start(`Custom ${myUserId}`);
```

Shortly after the above code runs, the *todoItems* array would have all the records from the *todo* table.

Next, let's invoke a method on our API to add a new *todo* record (use whatever client HTTP library you wish)...

```js
$.ajax('/api/todo/insert', {
  method: 'POST',
  data: JSON.stringify({
    name: 'My First To-Do',
    owner: 'Spongebob',
  }),
});
```

After the above code runs, the server will have a new *todo* record and a new *todo* record will automagically be added to the local *todoItems* array.

## More Complex Subscriptions

In the *Overview* example above, we subscribed to all the data in a single *todo* table; however, much more complex subscriptions are supported...

```cs
channelRoute.RegisterChannel("todo-page", handlerAsync: async(vars, channel) => {
  var dynamicViewSet = database.CreateDynamicViewSet(dataEventTransaction => {
    channel.Queue("DATA-EVENT-TRANSACTION", dataEventTransaction);
  });

  string userId = channel.Connection.AuthToken;

  dynamicViewSet.CreateDynamicView(
    @"SELECT td.id, td.name, td.user_id, u.name user_name
    FROM todo td
      INNER JOIN user u ON td.user_id=u.id
    WHERE u.id=@userId",
    new {
      userId
    },
    name: "todo",
    keyFieldNames: new string[] { "id" }	
  );

  dynamicViewSet.CreateDynamicView(
    @"SELECT id, name
    FROM tags
    WHERE user_id=@userId",
    new {
      userId
    }
  );

  return dynamicViewSet;
);
```

In this example, a client subscribing to *todo-page* will get a *todo* collection and a *tags* collection both filtered by user id.  

Because the new *todo* collection is the result of a join, the client will receive updates if changes to either of the underlying *todo* table or *user* table would change the collection results.


## Getting Started

You can either install the binaries from NuGet (just install the ones you need)...

```
nuget install Butterfly.Auth
nuget install Butterfly.Channel
nuget install Butterfly.Channel.EmbedIO
nuget install Butterfly.Database
nuget install Butterfly.Database.Memory
nuget install Butterfly.Database.MySQL
nuget install Butterfly.Notify
nuget install Butterfly.Notify.Aws
nuget install Butterfly.Notify.Twilio
nuget install Butterfly.Util
nuget install Butterfly.WebApi
nuget install Butterfly.WebApi.EmbedIO
```

Although there are a lot of packages, all the packages are very small in size (largest is less than 45kB).


Or you can get the source from GitHub...

1. Clone the github repository `https://github.com/fireshark/Butterfly.git`
1. Open `Butterfly.sln` in Visual Studio 2017
1. Click the Start button (will compile the solution and run the Butterfly.Examples project)
1. Open [http://localhost:8080/](http://localhost:8080) to view the examples

*Note:* The Better Chat example will only be available if `BetterChat.cs` has been configured to connect to a MySQL database.


## Packages

There are three key packages each with alternative implementations...

- [Butterfly.Database](https://firesharkstudios.github.io/Butterfly/Butterfly.Database) - Allows executing SELECT statements, creating transactions to execute INSERT, UPDATE, and DELETE statements; creating dynamic views; and receiving data change events both on tables and dynamic views.  This is the bread and butter of the Butterfly Framework.

  - [Butterfly.Database.Memory](https://firesharkstudios.github.io/Butterfly/Butterfly.Database.Memory) - In memory implementation (no persistence, no support for joins)
  - [Butterfly.Database.MySql](https://firesharkstudios.github.io/Butterfly/Butterfly.Database.MySql) - Implementation for MySql
  - [Butterfly.Database.Postgres](https://firesharkstudios.github.io/Butterfly/Butterfly.Database.Postgres) - Implementation for Postgres
  - [Butterfly.Database.SQLite](https://firesharkstudios.github.io/Butterfly/Butterfly.Database.SQLite) - Implementation for SQLIte
 
- [Butterfly.Channel](https://firesharkstudios.github.io/Butterfly/Butterfly.Channel) - Allows clients to create new channels to the server and allows the server to push messages to connected clients (think WebSockets).

  - [Butterfly.Channel.EmbedIO](https://firesharkstudios.github.io/Butterfly/Butterfly.Channel.EmbedIO) - Implementation for [EmbedIO](https://github.com/unosquare/embedio) server
  - [Butterfly.Channel.RedHttpServer](https://firesharkstudios.github.io/Butterfly/Butterfly.Channel.RedHttpServer) - Implementation for [RedHttpServer](https://github.com/rosenbjerg/RedHttpServer.CSharp)

- [Butterfly.WebApi](https://firesharkstudios.github.io/Butterfly/Butterfly.WebApi) - Allows receiving API requests via HTTP (inspired by Express JS) by wrapping existing C# web servers.

  - [Butterfly.WebApi.EmbedIO](https://firesharkstudios.github.io/Butterfly/Butterfly.WebApi.EmbedIO) - Implementation for [EmbedIO](https://github.com/unosquare/embedio) server
  - [Butterfly.WebApi.RedHttpServer](https://firesharkstudios.github.io/Butterfly/Butterfly.WebApi.RedHttpServer) - Implementation for [RedHttpServer](https://github.com/rosenbjerg/RedHttpServer.CSharp)

There are no interdependencies between the packages. Use any subset of the packages you wish.

## Contributing

If you'd like to contribute, please fork the repository and use a feature
branch. Pull requests are warmly welcome.

## Licensing

The code is licensed under the Apache License 2.0.  

