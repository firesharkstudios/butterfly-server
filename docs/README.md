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
                    return database.CreateAndStartDynamicViewAsync("SELECT * FROM todo", dataEventTransaction => channel.Queue(dataEventTransaction));
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
- Defines a [Subscription API](#creating-a-subscription-api) to subscribe to a *todos* subscription

Clients are expected to...
- Use the subscription API to subscribe to the *todos* subscription to get a list of all initial *todo* records and any changes to the *todo* records
- Use the defined web API to insert and delete *todo* records

See [Todo Server](https://github.com/firesharkstudios/butterfly-server-dotnet/tree/master/Butterfly.Example.Todo.Server) for the working server code.

## The Client

Now, let's see how a client might interact with this server using the [Butterfly Client](#butterfly-client) javascript library.

First, the client should use [WebSocketChannelClient](#websocketchannelclient) to maintain an open WebSocket to the server...

```js
let channelClient = new WebSocketChannelClient({
    url: `ws://${window.location.host}/ws`
});
channelClient.connect();
```

Next, the client will want to subscribe to a channel to receive data...

```js
let todosList = [];
channelClient.subscribe({
    channel: 'todos',
    handler: new ArrayDataEventHandler({
        arrayMapping: {
            todo: todosList
        }
    })
});
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

An [IWebApi](https://butterflyserver.io/docfx/api/Butterfly.Core.WebApi.IWebApi.html) instance allows defining an API for your application like this...

```cs
webApi.OnPost("/api/todo/insert", async (req, res) => {
    var todo = await req.ParseAsJsonAsync<Dict>();
    await database.InsertAndCommitAsync<string>("todo", todo);
});
webApi.OnPost("/api/todo/delete", async (req, res) => {
    var id = await req.ParseAsJsonAsync<string>();
    await database.DeleteAndCommitAsync("todo", id);
});

// Don't forget to compile
webApi.Compile();
```

You need an implementation like [EmbedIO](#using-embedio) to get an instance of [IWebApi](https://butterflyserver.io/docfx/api/Butterfly.Core.WebApi.IWebApi.html).

### Example Request Handling

There are many ways to receive data from a client...

```cs
webApi.OnGet("/api/todo/{id}", (req, res) => {
    // Opening /api/todo/123 would print id=123 below
    Console.WriteLine($"id={req.PathParams["id"]});
});

webApi.OnGet("/api/todo", (req, res) => {
    // Opening /api/todo?id=123 would print id=123 below
    Console.WriteLine($"id={req.QueryParams["id"]});
});

webApi.OnPost("/api/todo", async(req, res) => {
    // A javascript client posting JSON data with...
    //     $.ajax('/api/todo', {
    //         method: 'POST',
    //         data: JSON.stringify("123"),
    //     });
    // would echod id=123 below
    var data = await req.ParseAsJsonAsync<string>();
    Console.WriteLine($"id={data});
});

webApi.OnPost("/api/todo", async(req, res) => {
    // A javascript client posting JSON data with...
    //     $.ajax('/api/todo', {
    //         method: 'POST',
    //         data: JSON.stringify({ id: "123" }),
    //     });
    // would echod id=123 below
    var data = await req.ParseAsJsonAsync<Dictionary<string, string>>();
    Console.WriteLine($"id={data["id"]});
});

webApi.OnPost("/api/todo", async(req, res) => {
    // A javascript client posting JSON data with...
    //     $.ajax('/api/todo', {
    //         method: 'POST',
    //         data: JSON.stringify(["abc", "123"]),
    //     });
    // would echod id=123 below
    var data = await req.ParseAsJsonAsync<string[]>();
    Console.WriteLine($"id={data[1]});
});

webApi.OnPost("/api/todo", async(req, res) => {
    // A javascript client posting JSON data with...
    //     var formData = new FormData();
    //     formData.append("id", "123");
    //     $.ajax('/api/todo', {
    //         method: 'POST',
    //         data: formData,
    //     });
    // would echod id=123 below
    var data = await req.ParseAsUrlEncodedAsync();
    Console.WriteLine($"id={data["id"]});
});
```

See [IHttpRequest](https://butterflyserver.io/docfx/api/Butterfly.Core.WebApi.IHttpRequest.html) for more details.

### Example Response Handling

There are many ways to send a response to a client...

```cs
// Respond with plain text
webApi.OnPost("/api/todo", async(req, res) => {
    await res.WriteAsTextAsync("OK");
});

// Respond with JSON object
webApi.OnPost("/api/todo", async(req, res) => {
    await res.WriteAsJsonAsync(new {
        result = "OK"
    });
});

// Respond with JSON array
webApi.OnPost("/api/todo", async(req, res) => {
    await res.WriteAsJsonAsync(new string[] {
        "OK"
    });
});

// Redirect the client
webApi.OnGet("/api/todo/{id}", (req, res) => {
    res.SendRedirect("/api/todo/not-found");
});
```

See [IHttpResponse](https://butterflyserver.io/docfx/api/Butterfly.Core.WebApi.IHttpResponse.html) for more details.

## Creating a Subscription API

### Overview

An [ISubscriptionApi](https://butterflyserver.io/docfx/api/Butterfly.Core.WebApi.ISubscriptionApi.html) instance allows defining a Subscription API that can push real-time data to clients like this...

```cs
subscriptionApi.OnSubscribe("todos", (vars, channel) => {
    return database.CreateAndStartDynamicViewAsync("todo", dataEventTransaction => {
	    channel.Queue(dataEventTransaction);
    });
});
```

Notes
- The *vars* variable allows the client to pass values to the subscription
- The *channel* variable allows accessing the client authentication information
- The subscription handler must return an object implementing *IDisposable* (object will be disposed when the client unsuscribes) 

A common usecase is to return a *DynamicViewSet* instance that pushes initial data and data changes to the client over the channel.

You need an implementation like [EmbedIO](#using-embedio) to get an instance of [ISubscriptionApi](https://butterflyserver.io/docfx/api/Butterfly.Core.WebApi.ISubscriptionApi.html).

### Example Simple Subscription

The following javascript client subscribes to an *echo-messages* subscription passing in a *someName* variable and echoing the *messageType* and *message* received from the server to the console...

```js
// Javascript client
channelClient.subscribe({
    channel: 'echo-messages',
    vars: {
        someName = 'Spongebob',
    },
    handler(messageType, message) {
        console.debug(`messageType=${messageType},message=${message}`);
    })
});
```

The above code assumes you have [Butterfly Client](#butterfly-client) installed and have initialized a *WebSocketChannelClient* instance.

The following server code defines the *echo-messages* subscription that uses an instance of the *RunEvery* class to send a message to any subscribed clients every 2 seconds...

```cs
// C# server
subscriptionApi.OnSubscribe("echo-messages", async(vars, channel) => {
    int count = 0;
    var someName = vars.GetAs("someName", "");
    return Butterfly.Util.RunEvery(() => {
        channel.Queue("Echo", $"Message #{++count} from {someName}");
    }, 2000);
);
```

Notice that the subscription handler above returns the instance of *RunEvery* which implements *IDisposable*.  The *RunEvery* instance will be disposed when the client unsubscribes (or disconnects for too long).

So, the end result of running the code above would be the following in the client javascript console...

```js
messageType=Echo,message=Message #1 from Spongebob
messageType=Echo,message=Message #2 from Spongebob
messageType=Echo,message=Message #3 from Spongebob
...
```


### Example Dynamic Subscription

The following javascript client subscribes to a *todo-page* subscription and maps the two datasets to the local *todosList* and *tagsList* arrays...

```js
let todosList = [];
let tagsList = [];
channelClient.subscribe({
    channel: 'todo-page',
    vars: {
        userId: '123'
    },
    handler: new ArrayDataEventHandler({
        arrayMapping: {
            todo: todosList
            tag: tagsList
        }
    })
});
```

The above code assumes you have [Butterfly Client](#butterfly-client) installed and have initialized a *WebSocketChannelClient* instance.

The following server code defines the *todo-page* subscription that returns a *DynamicViewSet* containing two *DynamicViews* (one for *todos* and one for *tags*)...

```cs
subscriptionApi.OnSubscribe("todo-page", async(vars, channel) => {
    var dynamicViewSet = database.CreateDynamicViewSet(dataEventTransaction => channel.Queue(dataEventTransaction);

    string userId = vars.GetAs("userId", "");
    if (!string.IsNullOrEmpty(userId)) throw new Exception("Must specify a userId in vars");

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

    // Send initial datasets and send any data changes as they occur    
    await dynamicViewSet.StartAsync();

    return dynamicViewSet;
);
```

So, the end result of running the code above would be a local *todosList* and *tagsList* arrays that automatically stay synhcronized with the server.

## Accessing a Database

### Overview

An [IDatabase](https://butterflyserver.io/docfx/api/Butterfly.Core.Database.IDatabase.html) instance allows creating transactions, modifying data, retrieving data, and subscribing to data change events.

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

The [IDatabase](https://butterflyserver.io/docfx/api/Butterfly.Core.Database.IDatabase.html) instance also support transactions and the ability to publish data change events on tables and even complex SELECT statements.


### Using a Memory Database

[Butterfly.Core.Database.MemoryDatabase](https://butterflyserver.io/docfx/api/Butterfly.Core.Database.Memory.MemoryDatabase.html) database is included in [Butterfly.Core](api/Butterfly.Core.md).

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

### Selecting Data

Call *IDatabase.SelectRowsAsync()* with alternative values for the *sql* parameter and alternative values for the *vars* parameter...

```cs
var sql1 = "employee"; // SELECT statement is auto generated
var sql2 = "SELECT * FROM employee"; // WHERE statement is auto generated 
var sql3 = "SELECT * FROM employee WHERE id=@id"; // Specify exact SELECT statement

var vars1 = "123"; // Can specify just the primary key value
var vars2 = new { id = "123" }; // Can specify parameters using an anonymous type
var vars3 = new Dictionary<string, object> { { "id", "123" } }; // Can specify parameters using a Dictionary
 
// Any combination of sql1/sql2/sql3 and vars1/vars2/vars3 would yield identical results
Dictionary<string, object>[] rows = await database.SelectAsync(sql1, vars1);
```

Retrieve multiple rows, a single row, or a single value...

```cs
// Retrieve multiple rows
Dictionary<string, object>[] rows = await database.SelectRowsAsync("SELECT * FROM employee");

// Retrieve a single row 
Dictionary<string, object> row = await database.SelectRowAsync("SELECT * FROM employee", "123")

// Retrieve a single value
string name = await database.SelectValueAsync<string>("SELECT name FROM employee", "123")
```

The WHERE clause will be auto-generated or rewritten in specific scenarios...

```cs
// Executes WHERE department_id = '123'
Dictionary<string, object>[] rows = await database.SelectRowsAsync("employee", new {
	department_id = "123"
});

// Executes WHERE department_id IS NULL
Dictionary<string, object>[] rows = await database.SelectRowsAsync("employee", new {
	department_id = (string)null
});

// Executes WHERE department_id IS NOT NULL
Dictionary<string, object>[] rows = await database.SelectRowsAsync(
	"SELECT * employee WHERE department_id!=@did", 
	new {
		did = (string)null
	}
);

// Executes WHERE department_id IN ('123', '456')
Dictionary<string, object>[] rows = await database.SelectRowsAsync("employee", new {
	department_id = new string[] { "123", "456"}
});

// Executes WHERE department_id NOT IN ('123', '456')
Dictionary<string, object>[] rows = await database.SelectRowsAsync(
	"SELECT * employee WHERE department_id!=@did", 
	new {
		did = new string[] { "123", "456"}
	}
);
```
### Modifying Data

```cs
// Execute a single INSERT and return the value of the primary key
string id = database.InsertAndCommitAsync<string>("employee", new {
	first_name = "Jim",
	last_name = "Smith",
	balance = 0.0f,
});

// Assuming the employee table has a unique index on the id field, 
// this updates the balance field on the matching record
database.UpdateAndCommitAsync<string>("employee", new {
	id = "123",
	balance = 0.0f,
});

// Assuming the employee table has a unique index on the id field, 
// this deletes the matching record
database.DeleteAndCommitAsync<string>("employee", "123");

```
### Transactions

```cs
// If either INSERT fails, neither INSERT will be saved
using (ITransaction transaction = await database.BeginTransactionAsync()) {
	string departmentId = transaction.InsertAsync<string>("department", new {
		name = "Sales"
	});
	string employeeId = transaction.InsertAsync<string>("employee", new {
		name = "Jim Smith",
		department_id = departmentId,
	});
	await transaction.CommitAsync();
}
```
### Synchronizing Data

Use *ITransaction.SynchronizeAsync* to determine the right INSERT, UPDATE, and DELETE statements to synchronize two collections...

```cs
// Assumes an article_tag table with article_id and tag_name fields
public async Task SynchronizeTag(string articleId, string[] tagNames) {
	// First, create the existingRecords from the database
	Dict[] existingRecords = database.SelectRowsAsync(
		@"SELECT article_id, tag_name 
		FROM article_tag 
		WHERE article_id=@articleId",
		new {
			articleId
		}
	);

	// Next, create the newRecords collection from the tagNames parameter
	Dict[] newRecords = tagNames.Select(x => new Dictionary<string, object> {
		{ "article_id", articleId },
		{ "tag_name", x },
	});

	// Now, execute the right INSERT, UPDATE, and DELETE statements to make
	// the newRecords collection match the existingRecords collection
	using (ITransaction transaction = database.BeginTransactionAsync()) {
		bool changed = await transaction.SynchronizeAsync(
			"article_tag", 
			existingRecords, 
			newRecords, 
			existingRecord => new Dictionary<string, object> {
				article_id = existingRecord.GetAs("article_id", (string)null),
				tag_name = existingRecord.GetAs("tag_name", (string)null),
			},
		);
	}
}
```

### Defaults, Overrides, and Preprocessors

Can be defined globally or per table...

```cs
// Add an id field to all INSERTs with values like at_58b5fff4-322b-4fe8-b45d-386dac7a79f9
// if INSERTing on an auth_token table
database.SetDefaultValue(
    "id", 
    tableName => $"{tableName.Abbreviate()}_{Guid.NewGuid().ToString()}"
);

// Add a created_at field to all INSERTS with the current time
database.SetDefaultValue("created_at", tableName => DateTime.Now.ToUnixTimestamp());

// Remap any DateTime values to UNIX timestamp values
database.AddInputPreprocessor(BaseDatabase.RemapTypeInputPreprocessor<DateTime>(
    dateTime => dateTime.ToUnixTimestamp()
));

// Remap any $NOW$ values to the current UNIX timestamp
database.AddInputPreprocessor(BaseDatabase.RemapTypeInputPreprocessor<string>(
    text => text=="$NOW$" ? DateTime.Now.ToUnixTimestamp().ToString() : text
));

// Remap any $UPDATE_AT$ values to be the same value as the updated_at field
database.AddInputPreprocessor(BaseDatabase.CopyFieldValue("$UPDATED_AT$", "updated_at"));
```

### Dynamic Views

Dynamic views allow receiving both the initial data and data change events when the data changes.

```cs
// Create a DynamicViewSet that simply prints all data events to the console
using (DynamicViewSet dynamicViewSet = database.CreateDynamicViewSet(
	dataEventTransaction => Console.WriteLine(dataEventTransaction)
) {
	// Create dynamic view for all employees in department
	dynamicViewSet.CreateDynamicView("employee", new {
		department_id = "123"
	});

	// Create dynamic view for all resources in department
	dynamicViewSet.CreateDynamicView("resource", new {
		department_id = "123"
	});

	// This will cause each DynamicView above to execute
	// and all the initial data in each DynamicView to be
	// echoed to the console in a single DataEventTransaction
	dynamicViewSet.Start();

	// This will cause a new DataEventTransaction with a single
	// INSERT event to be echoed to the console
	database.InsertAndCommitAsync("employee", new {
		name = "Joe Smith",
		department_id = "123",
	});

	// This will NOT cause a new DataEventTransaction to
	// be printed to the console because this INSERT
	// does not change the DynamicView results above
	database.InsertAndCommitAsync("employee", new {
		name = "Joe Smith",
		department_id = "456",
	});
}
```

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

## Butterfly Client

### Installing

### WebSocketChannelClient

### ArrayDataEventHandler

### Vuex Bindings

# API Documentation

Click [here](https://butterflyserver.io/docfx/api/) for the API Documentation

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