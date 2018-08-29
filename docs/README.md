# Overview

This animation shows an [example](#example) application with three clients automatically sychronized with Butterfly Server .NET...

![Demo](https://raw.githubusercontent.com/firesharkstudios/butterfly-server-dotnet/master/img/demo.gif) 

Butterfly Server .NET provides...

- Ability to define a [Web API](#creating-a-web-api)
- Ability to define a [Subscription API](#creating-a-subscription-api) that allow pushing real-time data to clients
- Ability to modify, retrieve, and publish data change events on a [Database](#accessing-a-database)

Butterfly Server .NET targets *.NET Framework 2.0* and does **not** have any dependencies on ASP.NET.

# Getting Started

## Install from Nuget

| Name | Package | Install |
| --- | --- | --- |
| Butterfly.Core | [![nuget](https://img.shields.io/nuget/v/Butterfly.Core.svg)](https://www.nuget.org/packages/Butterfly.Core/) | `nuget install Butterfly.Core` |
| Butterfly.EmbedIO | [![nuget](https://img.shields.io/nuget/v/Butterfly.EmbedIO.svg)](https://www.nuget.org/packages/Butterfly.EmbedIO/) | `nuget install Butterfly.EmbedIO` |
| Butterfly.MySQL | [![nuget](https://img.shields.io/nuget/v/Butterfly.MySQL.svg)](https://www.nuget.org/packages/Butterfly.MySQL/) | `nuget install Butterfly.MySQL` |
| Butterfly.SQLite | [![nuget](https://img.shields.io/nuget/v/Butterfly.SQLite.svg)](https://www.nuget.org/packages/Butterfly.SQLite/) | `nuget install Butterfly.SQLite` |

## Install from Source Code

Get the source from [GitHub](https://github.com/firesharkstudios/butterfly-server-dotnet).

# Example

You can see an animation of running this example in the [Overview](#overview) section.

## Try It

Run this in a terminal or command prompt...

```
git clone https://github.com/firesharkstudios/butterfly-server-dotnet

cd butterfly-server-dotnet\Butterfly.Example.Todo.Server
dotnet run -vm
```

Run this in a second terminal or command prompt...

```
cd butterfly-server-dotnet\Butterfly.Example.Todo.Client
npm install
npm run dev
```

You should see http://localhost:8080/ open in a browser. Try opening a second browser instance at http://localhost:8080/. Notice that changes are automatically synchronized between the two browser instances.

There is also a [Cordova Todo Client](https://github.com/firesharkstudios/butterfly-server-dotnet/tree/master/Butterfly.Example.Todo.CordovaClient) and an [Electron Todo Client](https://github.com/firesharkstudios/butterfly-server-dotnet/tree/master/Butterfly.Example.Todo.ElectronClient).

## The Server

Here is all server code for our todo list manager...

```csharp
using System;

using Butterfly.Core.Util;

using Dict = System.Collections.Generic.Dictionary<string, object>;

namespace Butterfly.Example.HelloWorld.Server {
    class Program {
        static void Main(string[] args) {
            using (var embedIOContext = new Butterfly.EmbedIO.EmbedIOContext("http://+:8000/")) {
                // Create a MemoryDatabase (no persistence, limited features)
                var database = new Butterfly.Core.Database.Memory.MemoryDatabase();
                database.CreateFromText(@"CREATE TABLE todo (
	                id VARCHAR(50) NOT NULL,
	                name VARCHAR(40) NOT NULL,
	                PRIMARY KEY(id)
                );");
                database.SetDefaultValue("id", tableName => $"{tableName.Abbreviate()}_{Guid.NewGuid().ToString()}");

                // Listen for API requests
                embedIOContext.WebApi.OnPost("/api/todo/insert", async (req, res) => {
                    var todo = await req.ParseAsJsonAsync<Dict>();
                    await database.InsertAndCommitAsync<string>("todo", todo);
                });
                embedIOContext.WebApi.OnPost("/api/todo/delete", async (req, res) => {
                    var id = await req.ParseAsJsonAsync<string>();
                    await database.DeleteAndCommitAsync("todo", id);
                });

                // Listen for subscribe requests...
                // - The handler must return an IDisposable object (gets disposed when the channel is unsubscribed)
                // - The handler can push data to the client by calling channel.Queue()
                embedIOContext.SubscriptionApi.OnSubscribe("todos", (vars, channel) => {
                    return database.CreateAndStartDynamicView("SELECT * FROM todo", dataEventTransaction => channel.Queue(dataEventTransaction));
                });

                embedIOContext.Start();

                Console.ReadLine();
            }
        }
    }
}
```

The above C# code...
- Creates a Memory [database](#accessing-a-database) with a single *todo* table
- Defines a [Web API](#creating-a-web-api) to insert and delete *todo* records
- Defines a [Subscription API](#creating-a-subscription-api) to subscribe to a *todos* channel that retrieves all *todo* records **and** any changes to the *todo* records

See [Todo Server](https://github.com/firesharkstudios/butterfly-server-dotnet/tree/master/Butterfly.Example.Todo.Server) for the working server code.

## The Client

Now, let's see how a client might interact with this server using the Butterfly Client (`npm install butterfly-client`).

First, the client should maintain an open WebSocket to the server by using the *WebSocketChannelClient* class...

```js
let channelClient = new WebSocketChannelClient({
    url: `ws://${window.location.host}/ws`
});
channelClient.connect();
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

# Concepts

## Creating a Web Api

### Overview

[IWebApi](api/Butterfly.Core.WebApi/IWebApi) allows defining a RESTlike API using the HTTP GET and POST verbs like this...

```cs
webApi.OnPost("/api/todo/insert", async (req, res) => {
    var todo = await req.ParseAsJsonAsync<Dict>();
    await database.InsertAndCommitAsync<string>("todo", todo);
});
webApi.OnPost("/api/todo/delete", async (req, res) => {
    var id = await req.ParseAsJsonAsync<string>();
    await database.DeleteAndCommitAsync("todo", id);
});
```

This is "RESTlike API" because it's not following the standard practice of using HTTP Verbs to define the actions (which is often problematic with entities with a large number of actions). 

You need an implementation of [IWebApi](api/Butterfly.Core.WebApi/IWebApi) like [EmbedIO](#using-embedio).

### Defining Actions

Use [OnGet](api/Butterfly.Core.WebApi/IWebApi/OnGet) to respond to HTTP GET requests.

Use [OnPost](api/Butterfly.Core.WebApi/IWebApi/OnPost) to respond to HTTP POST requests.

## Creating a Subscription API

### Overview

[ISubscriptionApi](api/Butterfly.Core.Channel/ISubscriptionApi) allows defining a Subscription API that can push real-time data to clients like this...

```cs
subscriptionApi.OnSubscribe("todos", (vars, channel) => {
    return database.CreateAndStartDynamicViewAsync("todo", dataEventTransaction => {
	    channel.Queue(dataEventTransaction);
    });
});
```

In the example above, a subscription to the *todos* channel creates a *DynamicView* instance that pushes data changes over the channel to the client. 

You need an implementation of [ISubscriptionApi](api/Butterfly.Core.Channel/ISubscriptionApi) like [EmbedIO](#using-embedio).

### Defining Subscriptions

Example of a subscription returning multiple datasets and a dataset that uses a JOIN...

```cs
subscriptionApi.OnSubscribe("todo-page", async(vars, channel) => {
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

## Accessing a Database

### Overview

An [IDatabase](api/Butterfly.Core.Database/IDatabase) instance allows creating transactions, modifying data, retrieving data, and subscribing to data change events.

```cs
var id = await database.InsertAndCommitAsync<string>("todo", new {
    name = "My Todo"
});
await database.UpdateAndCommitAsync("todo", new {
    id,
    name = "My New Todo"
});
await database.DeleteAndCommitAsync("todo", id);

var name = await database.SelectValueAsync<string>("SELECT name FROM todo", id);
```

The [IDatabase](api/Butterfly.Core.Database/IDatabase) instance also support transactions and the ability to publish data change events on tables and even complex SELECT statements.


### Using a Memory Database

The Butterfly.Core.Database.Memory database is included with Butterfly.Core.

In your application...

```csharp
var database = new Butterfly.Core.Database.Memory.MemoryDatabase();
database.CreateFromText(@"CREATE TABLE todo (
	id VARCHAR(50) NOT NULL,
	name VARCHAR(40) NOT NULL,
	PRIMARY KEY(id)
);");
```

### Creating the Database

You can either create the database structure by...

- Executing CreateFromTextAsync() or CreateFromResourceAsync() in Butterfly Server .NET
- Creating the database yourself outside of Butterfly Server .NET

## Implementations

### Using EmbedIO

[EmbedIO](https://github.com/unosquare/embedio) is a capable low footprint web server that can be used to implement both the *IWebApi* and *ISubscriptionApi* interfaces. 

The *EmbedIOContext* class is a convenience class that creates IWebApi and ISubscriptionApi instances from a running EmbedIO web server.

In the *Package Manager Console*...

```
Install-Package Butterfly.EmbedIO
```

In your application...

```csharp
var embedIOContext = new Butterfly.EmbedIO.EmbedIOContext("http://+:8000/");

// Declare your Web API and Subscription API like...
embedIOContext.WebApi.OnPost("/api/todo/insert", async (req, res) => {
   // Do something
});
embedIOContext.WebApi.OnPost("/api/todo/delete", async (req, res) => {
   // Do something
});
embedIOContext.SubscriptionApi.OnSubscribe("todos", (vars, channel) => {
   // Do something
});

embedIOContext.Start();
```

### Using MySQL

In the *Package Manager Console*...

```
Install-Package Butterfly.MySql
```

In your application...

```csharp
var database = new Butterfly.MySql.MySqlDatabase("Server=127.0.0.1;Uid=test;Pwd=test!123;Database=butterfly_db_demo");
```

### Using SQLite

In the *Package Manager Console*...

```
Install-Package Butterfly.SQLite
```

In your application...

```csharp
var database = new Butterfly.SQLite.SQLiteDatabase("Filename=./my_database.db");
```

# Packages

Here are the key packages in *Butterfly.Core*...

- [Butterfly.Core.Auth](https://github.com/firesharkstudios/butterfly-server-dotnet/tree/master/docs/Butterfly.Core.Auth) - Allows registering and logging in users, handling forgot password and reset password requests, and validating auth tokens.
- [Butterfly.Core.Channel](https://github.com/firesharkstudios/butterfly-server-dotnet/tree/master/docs/Butterfly.Core.Channel) - Allows clients to create new channels to the server and allows the server to push messages to connected clients (think WebSockets).
- [Butterfly.Core.Database](https://github.com/firesharkstudios/butterfly-server-dotnet/tree/master/docs/Butterfly.Core.Database) - Allows executing SELECT statements, creating transactions to execute INSERT, UPDATE, and DELETE statements; creating dynamic views; and receiving data change events both on tables and dynamic views.  This is the bread and butter of the Butterfly Server .NET.
- [Butterfly.Core.Notify](https://github.com/firesharkstudios/butterfly-server-dotnet/tree/master/docs/Butterfly.Core.Notify) - Allows sending notifications (email/texts) to users.
- [Butterfly.Core.WebApi](https://github.com/firesharkstudios/butterfly-server-dotnet/tree/master/docs/Butterfly.Core.WebApi) - Allows receiving API requests via HTTP (inspired by Express JS) by wrapping existing C# web servers.

Here are various implementations you'll likely find useful...

- [Butterfly.MySql](https://github.com/firesharkstudios/butterfly-server-dotnet/tree/master/docs/Butterfly.MySql) - Implementation of *Butterfly.Core.Database* for MySql
- [Butterfly.EmbedIO](https://github.com/firesharkstudios/butterfly-server-dotnet/tree/master/docs/Butterfly.EmbedIO) - Implementation of *Butterfly.Core.Channel* and *Butterfly.Core.WebApi* for [EmbedIO](https://github.com/unosquare/embedio) server

# In the Wild

[Build Hero](https://www.buildhero.io) is a collaborative tool for general contractors, subcontractors, and customers to collaborate on remodel projects.  The [my.buildhero.io](https://my.buildhero.io) site, the Android app, and the iOS app are all powered by Butterfly Server .NET.

# Similar Projects

- [dotNetify](https://github.com/dsuryd/dotNetify)
- [SignalR](https://github.com/SignalR/SignalR)
- [SignalW](https://github.com/Spreads/SignalW)

# Contributing

If you'd like to contribute, please fork the repository and use a feature
branch. Pull requests are warmly welcome.

# Licensing

The code is licensed under the [Mozilla Public License 2.0](http://mozilla.org/MPL/2.0/).