
# Overview

![Demo](https://raw.githubusercontent.com/firesharkstudios/butterfly-server-dotnet/master/img/demo.gif) 

Butterfly Server .NET allows...

- Defining a [Web API](#creating-a-web-api)
- Defining a [Subscription API](#creating-a-subscription-api) that allow pushing real-time data to clients
- Subscribing to data change events on [Databases](#accessing-a-database) and [Dynamic Views](#using-dynamic-views)

Also, Butterfly Server .NET...

- Targets *.NET Framework 2.0*
- Fully supports async/await
- Does **not** depend on ASP.NET
- Does **not** use polling

An article creating a simple real-time chat app with [Vue.js](https://vuejs.org/) can be found [here](https://medium.com/@kent_19698/build-a-real-time-chat-app-from-scratch-using-vue-js-and-c-in-5-minutes-599387bdccbb).

# Getting Started

## Install from Nuget

| Name | Package | Install |
| --- | --- | --- |
| Butterfly.Core | [![nuget](https://img.shields.io/nuget/v/Butterfly.Core.svg)](https://www.nuget.org/packages/Butterfly.Core/) | `nuget install Butterfly.Core` |
| Butterfly.EmbedIO | [![nuget](https://img.shields.io/nuget/v/Butterfly.EmbedIO.svg)](https://www.nuget.org/packages/Butterfly.EmbedIO/) | `nuget install Butterfly.EmbedIO` |
| Butterfly.RedHttpServer | [![nuget](https://img.shields.io/nuget/v/Butterfly.RedHttpServer.svg)](https://www.nuget.org/packages/Butterfly.RedHttpServer/) | `nuget install Butterfly.RedHttpServer` |
| Butterfly.MySQL | [![nuget](https://img.shields.io/nuget/v/Butterfly.MySQL.svg)](https://www.nuget.org/packages/Butterfly.MySQL/) | `nuget install Butterfly.MySQL` |
| Butterfly.Postgres | [![nuget](https://img.shields.io/nuget/v/Butterfly.Postgres.svg)](https://www.nuget.org/packages/Butterfly.Postgres/) | `nuget install Butterfly.Postgres` |
| Butterfly.SQLite | [![nuget](https://img.shields.io/nuget/v/Butterfly.SQLite.svg)](https://www.nuget.org/packages/Butterfly.SQLite/) | `nuget install Butterfly.SQLite` |
| Butterfly.SqlServer | [![nuget](https://img.shields.io/nuget/v/Butterfly.SqlServer.svg)](https://www.nuget.org/packages/Butterfly.SqlServer/) | `nuget install Butterfly.SqlServer` |

## Install from Source Code

Get the source from [GitHub](https://github.com/firesharkstudios/butterfly-server-dotnet).

# Examples

You can try these examples...

- [Hello World](https://github.com/firesharkstudios/butterfly-server-dotnet/tree/master/Butterfly.Example.HelloWorld) - Shows *Hello World* in an alert box on the client
- [Database](https://github.com/firesharkstudios/butterfly-server-dotnet/tree/master/Butterfly.Example.Database) - Shows data change events on a [Dynamic View](#using-dynamic-views) in a console
- [Contact Manager](https://github.com/firesharkstudios/butterfly-server-dotnet/tree/master/Butterfly.Example.Crud) - Shows a simple CRUD web app using [Vuetify](https://vuetifyjs.com) on the client
- [Todo Manager](https://github.com/firesharkstudios/butterfly-server-dotnet/tree/master/Butterfly.Example.Todo) - Shows a simple *Todo* web app using [Vuetify](https://vuetifyjs.com) on the client

## Try It

Run this in a terminal or command prompt...

```
git clone https://github.com/firesharkstudios/butterfly-server-dotnet

cd butterfly-server-dotnet\Butterfly.Example.Todo
dotnet run -vm
```

Run this in a second terminal or command prompt...

```
cd butterfly-server-dotnet\Butterfly.Example.Todo\www
npm install
npm run dev
```

You should see http://localhost:8080/ open in a browser. Try opening a second browser instance at http://localhost:8080/. Notice that changes are automatically synchronized between the two browser instances.

Click [here](https://github.com/firesharkstudios/butterfly-server-dotnet/tree/master/Butterfly.Example.Todo) to see instructions for the Cordova and Electron clients.

# Concepts

## Working with Dictionaries

Since *Dictionary<string, object>* is used so extensively, you'll likely find it useful to declare an alias with your other *using* statements...

```cs
using Dict = System.Collections.Generic.Dictionary<string, object>;
```

*Butterfly.Core.Util* contains a [GetAs](https://butterflyserver.io/docfx/api/Butterfly.Core.Util.DictionaryX.html#Butterfly_Core_Util_DictionaryX_GetAs__3_Dictionary___0___1____0___2_) extension method for *Dict* that makes it easier to convert values...

Here are a few common scenarios related to database records...

```cs
// Retrieve from the todo table using the primary key value
Dict row = await database.SelectRowAsync("todo", "123");

// Retrieve as string
var id = row.GetAs("id", "");

// Retrieve as integer
var count = row.GetAs("count", -1);

// Retrieve as float
var amount = row.GetAs("id", 0.0f);

// Retrieve as DateTime instance (auto converts UNIX timestamp)
var createdAt = row.GetAs("created_at", DateTime.MinValue);
```

Here are a couple common scenarios related to the Web API...

```cs
webApi.OnPost("/api/todo/insert", async (req, res) => {
    var todo = await req.ParseAsJsonAsync<Dict>();

    // Retrieve as array
    var tags = todo.GetAs<string[]>("tags", null);

    // Retrieve as dictionary
    var options = todo.GetAs<Dict>("options", null);
});
```

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

You need an implementation like [EmbedIO](#using-embedio) or [RedHttpServer](#using-redhttpserver) to get an instance of [IWebApi](https://butterflyserver.io/docfx/api/Butterfly.Core.WebApi.IWebApi.html).

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

An [ISubscriptionApi](https://butterflyserver.io/docfx/api/Butterfly.Core.Channel.ISubscriptionApi.html) instance allows defining a Subscription API that can push real-time data to clients like this...

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

You need an implementation like [EmbedIO](#using-embedio) to get an instance of [ISubscriptionApi](https://butterflyserver.io/docfx/api/Butterfly.Core.Channel.ISubscriptionApi.html).

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
subscriptionApi.OnSubscribe("echo-messages", (vars, channel) => {
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

So, the end result of running the code above would be a local *todosList* and *tagsList* arrays that automatically stay synchronized with the server.

## Accessing a Database

### Overview

An [IDatabase](https://butterflyserver.io/docfx/api/Butterfly.Core.Database.IDatabase.html) instance allows modifying data, selecting data, and creating *DynamicViews*.

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


### Creating the Database Structure

You can create the database structure by either...

- Executing [CreateFromTextAsync()](https://butterflyserver.io/docfx/api/Butterfly.Core.Database.IDatabase.html#Butterfly_Core_Database_IDatabase_CreateFromTextAsync_System_String_) or [CreateFromResourceAsync()](https://butterflyserver.io/docfx/api/Butterfly.Core.Database.IDatabase.html#Butterfly_Core_Database_IDatabase_CreateFromResourceFileAsync_Assembly_System_String_) in Butterfly Server .NET (most useful for MemoryDatabase)
- Creating the database yourself outside of Butterfly Server .NET (normally recommended)

### Selecting Data

There are three flavors of selecting data with different return values...

| Method | Description |
| --- | --- |
| [SelectRowsAsync()](https://butterflyserver.io/docfx/api/Butterfly.Core.Database.IDatabase.html#Butterfly_Core_Database_IDatabase_SelectRowsAsync_System_String_System_Object_System_Int32_) | Returns an array of *Dict* instances |
| [SelectRowAsync()](https://butterflyserver.io/docfx/api/Butterfly.Core.Database.IDatabase.html#Butterfly_Core_Database_IDatabase_SelectRowAsync_System_String_System_Object_) | Returns a single *Dict* instances |
| [SelectValueAsync<T>()](https://butterflyserver.io/docfx/api/Butterfly.Core.Database.IDatabase.html#Butterfly_Core_Database_IDatabase_SelectValueAsync__1_System_String_System_Object___0_) | Returns a single value |

Each flavor above takes a *sql* parameter and optional *values* parameter.

The *sql* parameter can be specified in multiple ways...

| Name | Example Value |
| --- | --- | --- |
| Table name only | `"todo"` |
| SELECT without WHERE | `"SELECT * FROM todo"` |
| Full SELECT| `"SELECT * FROM todo WHERE id=@id"` |

The *values* parameter can also be specified in multiple ways...

| Name | Example Value |
| --- | --- | --- |
| Anonymous type | `new { id = "123" }` |
| Dictionary | `new Dictionary<string, object> { ["id"] = "123" }` |
| Primary Key Value | `"123"` |

Specific value types will also cause a WHERE clause to be rewritten as follows...

| Original WHERE | Values | New WHERE |
| --- | --- | --- |
| WHERE test=@test | `new { test = (string)null }` | WHERE test IS NULL |
| WHERE test!=@test | `new { test = (string)null }` | WHERE test IS NOT NULL |
| WHERE test=@test | `new { test = new string[] {"123","456") }` | WHERE test IN ('123', '456') |
| WHERE test!=@test | `new { test = new string[] {"123","456") }` | WHERE test NOT IN ('123', '456') |

So, these are all valid examples...

```cs
// Both of these effectively run SELECT * FROM employee
Dict[] allEmployees1 = await database.SelectRowsAsync("employee");
Dict[] allEmployees2 = await database.SelectRowsAsync("SELECT * FROM employee");

// Both of these effectively run SELECT * FROM employee WHERE department_id="123"_
Dict[] departmentEmployees1 = await database.SelectRowsAsync("employee", new {
    department_id = "123"
});
Dict[] departmentEmployees1 = await database.SelectRowsAsync("employee", new Dict {
    { "department_id", "123" }
});

// All three of these effectively run SELECT name FROM employee WHERE id='123'
string name1 = await database.SelectValueAsync<string>("SELECT name FROM employee", "123");
string name2 = await database.SelectValueAsync<string>("SELECT name FROM employee", new {
    id = "123"
});
string name3 = await database.SelectValueAsync<string>("SELECT name FROM employee", new Dict {
    { "id", "123" },
});

// Effectively runs SELECT * FROM employee WHERE department_id IS NULL
Dict[] rows = await database.SelectRowsAsync("employee", new {
    department_id = (string)null
});

// Effectively runs SELECT * FROM employee WHERE department_id IS NOT NULL
Dict[] rows = await database.SelectRowsAsync("SELECT * employee WHERE department_id!=@department_id", new {
    department_id = (string)null
});

// Effectively runs SELECT * FROM employee WHERE department_id IN ('123', '456')
Dict[] rows = await database.SelectRowsAsync("employee", new {
    department_id = new string[] { "123", "456"}
});

// Effectively runs SELECT * FROM employee WHERE department_id NOT IN ('123', '456')
Dict[] rows = await database.SelectRowsAsync("SELECT * employee WHERE department_id!=@department_id", new {
    department_id = new string[] { "123", "456"}
});
```
### Modifying Data

A [IDatabase](https://butterflyserver.io/docfx/api/Butterfly.Core.Database.IDatabase.html) instance has convenience methods that create a transaction, perform a specific action, and commit the transaction as follows...

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

In addition, you can explicitly create and commit a transaction that performs multiple actions...

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

    // Don't forget to Commit the transaction
	await transaction.CommitAsync();
}
```

Sometimes, it's useful to run code after a transaction is committed, this can be done using [OnCommit](https://butterflyserver.io/docfx/api/Butterfly.Core.Database.ITransaction.html#Butterfly_Core_Database_ITransaction_OnCommit_Func_Task__) to register an action that will execute after the transaction is committed.

### Synchronizing Data

It's common to synchronize a set of records in the database with a new set of inputs.  

The [SynchronizeAsync](https://butterflyserver.io/docfx/api/Butterfly.Core.Database.ITransaction.html#Butterfly_Core_Database_ITransaction_SynchronizeAsync_System_String_System_Collections_Generic_Dictionary_System_String_System_Object____System_Collections_Generic_Dictionary_System_String_System_Object____Func_System_Collections_Generic_Dictionary_System_String_System_Object__System_Object__System_String___) can be used to determine the right INSERT, UPDATE, and DELETE statements to synchronize two collections...

```cs
// Assumes an article_tag table with article_id and tag_name fields
public async Task SynchronizeTags(string articleId, string[] tagNames) {
    // First, retrieve the existing records from the database
    Dict[] existingRecords = database.SelectRowsAsync(
        @"SELECT article_id, tag_name 
        FROM article_tag 
        WHERE article_id=@articleId",
        new {
            articleId
        }
    );

    // Next, create the new records collection from the tagNames parameter
    Dict[] newRecords = tagNames.Select(x => new Dict {
        { "article_id", articleId },
        { "tag_name", x },
    }).ToArray();

    // Now, execute SynchronizeAsync() to determine the right 
    // INSERT, UPDATE, and DELETE statements to make the collections match
    using (ITransaction transaction = database.BeginTransactionAsync()) {
        await transaction.SynchronizeAsync(
            "article_tag", 
            existingRecords, 
            newRecords
        );
    }
}
```

### Defaults, Overrides, and Preprocessors

A [IDatabase](https://butterflyserver.io/docfx/api/Butterfly.Core.Database.IDatabase.html) instance allows defining...

- Default Values (applies to INSERTs)
- Override Values (applies to INSERTs and UPDATEs)
- Input Proprocessors

Each can be defined globally or per table.

Examples...

```cs
// Add an id field to any INSERT with values like at_58b5fff4-322b-4fe8-b45d-386dac7a79f9
// if INSERTing on an auth_token table
database.SetDefaultValue(
    "id", 
    tableName => $"{tableName.Abbreviate()}_{Guid.NewGuid().ToString()}"
);

// Add a created_at field to any INSERT with the current time
database.SetDefaultValue("created_at", tableName => DateTime.Now.ToUnixTimestamp());

// Add an updated_at field to any INSERT or UPDATE with the current time
this.database.SetOverrideValue("updated_at", tableName => DateTime.Now.ToUnixTimestamp());

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

## Using Dynamic Views

### Overview

A [DynamicViewSet](https://butterflyserver.io/docfx/api/Butterfly.Core.Database.Dynamic.DynamicViewSet.html) allows...

- Defining multiple [DynamicView](https://butterflyserver.io/docfx/api/Butterfly.Core.Database.Dynamic.DynamicView.html) instances using a familiar SELECT syntax
- Publishing the initial rows as a single [DataEventTransaction](https://butterflyserver.io/docfx/api/Butterfly.Core.Database.Event.DataEventTransaction.html) instance
- Publishing any changes as new [DataEventTransaction](https://butterflyserver.io/docfx/api/Butterfly.Core.Database.Event.DataEventTransaction.html) instances

Each [DynamicView](https://butterflyserver.io/docfx/api/Butterfly.Core.Database.Dynamic.DynamicView.html) instance must...

- Have a unique name (defaults to the first table name in the SELECT) within a [DynamicViewSet](https://butterflyserver.io/docfx/api/Butterfly.Core.Database.Dynamic.DynamicViewSet.html)
- Have key field(s) that uniquely identify each row (defaults to the primary key of the first table in the SELECT) 

You can use the [Butterfly Client](#butterfly-client) libraries to consume these [DataEventTransaction](https://butterflyserver.io/docfx/api/Butterfly.Core.Database.Event.DataEventTransaction.html) instances to keep local javascript arrays synchronized with your server.

Key limitations...

- Only INSERTs, UPDATEs, and DELETEs executed via an [IDatabase](https://butterflyserver.io/docfx/api/Butterfly.Core.Database.IDatabase.html) instance will trigger data change events
- SELECT statements with UNIONs are not supported
- SELECT statements with subqueries may not be supported depending on the type of subquery
- SELECT statements with multiple references to the same table can only trigger updates on one of the references

A [DynamicView](https://butterflyserver.io/docfx/api/Butterfly.Core.Database.Dynamic.DynamicView.html) will execute additional modified SELECT statements on each underlying data change event.  These modified SELECT statements are designed to execute quickly (always includes a primary key of an underlying table); however, this is additional overhead that should be considered on higher traffic implementations.

### Example

Here is an example of creating a [DynamicViewSet](https://butterflyserver.io/docfx/api/Butterfly.Core.Database.Dynamic.DynamicViewSet.html) and triggering [DataEventTransaction](https://butterflyserver.io/docfx/api/Butterfly.Core.Database.Event.DataEventTransaction.html) instances by starting the [DynamicViewSet](https://butterflyserver.io/docfx/api/Butterfly.Core.Database.Dynamic.DynamicViewSet.html) and by executing an INSERT...
```cs
var dynamicViewSet = database.CreateAndStartDynamicViewAsync(
    @"SELECT t.id, t.name todo_name, u.name user_name
    FROM todo t 
        INNER JOIN user u ON t.user_id=u.id
    WHERE is_done=@isDoneFilter",
    dataEventTransaction => {
        var json = JsonUtil.Serialize(dataEventTransaction, format: true);
        Console.WriteLine($"dataEventTransaction={json}");
    },
    new {
        isDoneFilter = "Y"
    }
);
dynamicViewSet.Start();
```

The above code would cause a [DataEventTransaction](https://butterflyserver.io/docfx/api/Butterfly.Core.Database.Event.DataEventTransaction.html) like this to be echoed to the console...

```js
dataEventTransaction={
  "dateTime": "2018-08-24 14:25:59",
  "dataEvents": [
    {
      "name": "todo",
      "keyFieldNames": [
        "id"
      ],
      "dataEventType": "InitialBegin",
      "id": "f916082a-7e56-4974-8bce-9c0af0792362"
    },
    {
      "record": {
        "id": "t_7dcdaf99-50ab-4bd5-ab26-271974e9cc49",
        "todo_name": "Todo #4",
        "user_name": "Patrick"
      },
      "name": "todo",
      "keyValue": "t_7dcdaf99-50ab-4bd5-ab26-271974e9cc49",
      "dataEventType": "Initial",
      "id": "134afc7e-a24e-448a-b800-baed7774d6d2"
    },
    {
      "record": {
        "id": "t_0f2c7147-317b-4f70-851c-dc906db6f2c3",
        "todo_name": "Todo #1",
        "user_name": "Spongebob"
      },
      "name": "todo",
      "keyValue": "t_0f2c7147-317b-4f70-851c-dc906db6f2c3",
      "dataEventType": "Initial",
      "id": "aaa6e491-5ad4-4a2b-9891-b1d402172c46"
    },
    {
      "record": {
        "id": "t_e71e3d82-2153-4b1b-8fcd-29815805307b",
        "todo_name": "Todo #2",
        "user_name": "Spongebob"
      },
      "name": "todo",
      "keyValue": "t_e71e3d82-2153-4b1b-8fcd-29815805307b",
      "dataEventType": "Initial",
      "id": "efea5a4b-9a9c-4bea-bc19-d6a460f27abb"
    },
    {
      "dataEventType": "InitialEnd",
      "id": "f25b8841-b9a3-4ec6-af0a-3d34687fa767"
    }
  ]
}
```

Now, let's add a record that impacts our [DynamicViewSet](https://butterflyserver.io/docfx/api/Butterfly.Core.Database.Dynamic.DynamicViewSet.html)...

```cs
await database.InsertAndCommitAsync<string>("todo", new {
    name = "Task #5",
    user_id = spongebobId,
    is_done = "N",
});
```

The above code would trigger the following [DataEventTransaction](https://butterflyserver.io/docfx/api/Butterfly.Core.Database.Event.DataEventTransaction.html) to be echoed to the console...

```js
dataEventTransaction={
  "dateTime": "2018-08-24 14:25:59",
  "dataEvents": [
    {
      "record": {
        "id": "t_89378473-97ed-4e0f-9c1d-4303ef6f4d04",
        "todo_name": "Task #5",
        "user_name": "Spongebob"
      },
      "name": "todo",
      "keyValue": "t_89378473-97ed-4e0f-9c1d-4303ef6f4d04",
      "dataEventType": "Insert",
      "id": "e140185e-9636-45e9-9687-a3368ad6caeb"
    }
  ]
}
```

You can run a more robust example [here](https://github.com/firesharkstudios/butterfly-server-dotnet/blob/master/Butterfly.Example.Database/Program.cs).

## Implementations

### Using a Memory Database

[Butterfly.Core.Database.MemoryDatabase](https://butterflyserver.io/docfx/api/Butterfly.Core.Database.Memory.MemoryDatabase.html) database is included in [Butterfly.Core](api/Butterfly.Core.md) and doesn't require installing additional packages; however, *MemoryDatabase* has these key limitattions...

- Data is NOT persisted
- SELECT statements with JOINs are NOT supported

Under the hood, the *MemoryDatabase* is using a System.Data.DataTable instance to manage the data.

In your application...

```csharp
var database = new Butterfly.Core.Database.Memory.MemoryDatabase();
```

### Using EmbedIO

[EmbedIO](https://github.com/unosquare/embedio) is a capable low footprint web server that can be used to implement both the [IWebApi](https://butterflyserver.io/docfx/api/Butterfly.Core.WebApi.IWebApi.html) and *ISubscriptionApi* interfaces. 

The [EmbedIOContext](https://butterflyserver.io/docfx/api/Butterfly.EmbedIO.EmbedIOContext.html) class is a convenience class that creates [IWebApi](https://butterflyserver.io/docfx/api/Butterfly.Core.WebApi.IWebApi.html) and ISubscriptionApi instances using an EmbedIO web server.

In the *Package Manager Console*...

```
Install-Package Butterfly.EmbedIO
```

In your application...

```csharp
var context = new Butterfly.EmbedIO.EmbedIOContext("http://+:8000/");

// Declare your Web API and Subscription API like...
context.WebApi.OnPost("/api/todo/insert", async (req, res) => {
   // Do something
});
context.WebApi.OnPost("/api/todo/delete", async (req, res) => {
   // Do something
});
context.SubscriptionApi.OnSubscribe("todos", (vars, channel) => {
   // Do something
});

context.Start();
```

### Using RedHttpServer

[RedHttpServer](https://github.com/rosenbjerg/Red) is a Kestrel/ASP.NET Core based web server that can be used to implement both the [IWebApi](https://butterflyserver.io/docfx/api/Butterfly.Core.WebApi.IWebApi.html) and [ISubscriptionApi](https://butterflyserver.io/docfx/api/Butterfly.Core.Channel.ISubscriptionApi.html) interfaces. 

The [RedHttpServerContext](https://butterflyserver.io/docfx/api/Butterfly.RedHttpServer.RedHttpServerContext.html) class is a convenience class that creates [IWebApi](https://butterflyserver.io/docfx/api/Butterfly.Core.WebApi.IWebApi.html) and [ISubscriptionApi](https://butterflyserver.io/docfx/api/Butterfly.Core.Channel.ISubscriptionApi.html) instances using RedHttpServer.

In the *Package Manager Console*...

```
Install-Package Butterfly.RedHttpServer
```

In your application...

```csharp
var context = new Butterfly.RedHttpServer.RedHttpServerContext("http://+:8000/");

// Declare your Web API and Subscription API like...
context.WebApi.OnPost("/api/todo/insert", async (req, res) => {
   // Do something
});
context.WebApi.OnPost("/api/todo/delete", async (req, res) => {
   // Do something
});
context.SubscriptionApi.OnSubscribe("todos", (vars, channel) => {
   // Do something
});

context.Start();
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

### Using Postgres

In the *Package Manager Console*...

```
Install-Package Butterfly.Postgres
```

In your application...

```csharp
var database = new Butterfly.Postgres.PostgresDatabase("User ID=test;Password=test!123;Host=localhost;Port=5432;Database=test;");
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

### Using MS SQL Server

In the *Package Manager Console*...

```
Install-Package Butterfly.SqlServer
```

In your application...

```csharp
var database = new Butterfly.SqlServer.SqlServerDatabase("Server=localhost; Initial Catalog=Butterfly; User ID=test; Password=test!123");
```

## Butterfly Client

### Overview

Butterfly Client is a javascript library that allows...

- Maintaining a connection to your server to receive subscription messages
- Mapping subscription messages received to synchronize local javascript arrays with the server

The easiest way to install Butterfly Client is with npm...

```
npm install butterfly-client
```

You can then include the Butterfly Client with a script import like...

```html
<script src="./node_modules/butterfly-client/lib/butterfly-client.js"></script>
```

Or include the classes you need with an appropriate ES6 import like...

```
import { ArrayDataEventHandler, WebSocketChannelClient } from 'butterfly-client'
```

### Example

An *WebSocketChannelClient* instance maintains a connection to your server to receive subscription messages and an *ArrayDataEventHandler* instance maps the subscription messages to local javascript arrays to keep these local javascript arrays synchronized with your server...

```js
let channelClient = new WebSocketChannelClient({
    url: `ws://localhost:8080/ws`,
    onStateChange(newState) {
        console.debug(`newState=${newState}`);
    },
    onSubscriptionsUpdated(newSubscriptions) {
        console.debug(`newSubscriptions=${newSubscriptions}`);
    },
});
channelClient.connect('Authorization : Bearer xyz');

let list1 = [];
let list2 = [];
channelClient.subscribe(
    channel: 'todos',
    handler: new ArrayDataEventHandler({
        channel: 'my-channel',
        vars: {
            someInfo: 'Some Info',
        },
        arrayMapping: {
            tableName1: list1,
            tableName2: list2,
        }
    })
);
```

# API Documentation

Click [here](https://butterflyserver.io/docfx/api/) for the API Documentation

# Running on Raspberry Pi

These steps worked to get Butterfly Server .NET running on a *Raspberry Pi 3 B+*...

```
# Install dotnet (per https://www.hanselman.com/blog/BuildingRunningAndTestingNETCoreAndASPNETCore21InDockerOnARaspberryPiARM32.aspx)...

sudo apt-get -y update
sudo apt-get -y install libunwind8 gettext
wget https://dotnetcli.blob.core.windows.net/dotnet/Sdk/2.1.300-rc1-008673/dotnet-sdk-2.1.300-rc1-008673-linux-arm.tar.gz
sudo mkdir /opt/dotnet
sudo tar -xvf dotnet-sdk-2.1.300-rc1-008673-linux-arm.tar.gz -C /opt/dotnet/
sudo ln -s /opt/dotnet/dotnet /usr/local/bin
dotnet --info

# Install node (per https://linux.tips/tutorials/how-to-install-latest-version-of-node-js-on-raspberry-pi-3)...

sudo -i
curl -L https://git.io/n-install | bash
. /root/.bashrc
exit

# Create server...

sudo mkdir -p /opt/chat
cd /opt/chat
sudo dotnet new console
sudo dotnet add package Butterfly.Core
sudo dotnet add package Butterfly.EmbedIO

# Copy and paste Program.cs from https://medium.com/@kent_19698/build-a-real-time-chat-app-from-scratch-using-vue-js-and-c-in-5-minutes-599387bdccbb
# Edit Program.cs to replace "../../../www" with args[0]

# Create www site...

sudo -i
mkdir www
cd www
npm init
npm install vue reqwest butterfly-client
exit

# Copy and paste index.html from https://medium.com/@kent_19698/build-a-real-time-chat-app-from-scratch-using-vue-js-and-c-in-5-minutes-599387bdccbb

# Run it...

sudo dotnet run /opt/chat/www

# Open http://<raspberry-pi-ip-address>:8000 from any machine on same network
```

# In the Wild

[Build Hero](https://www.buildhero.io) is a collaborative tool for general contractors, subcontractors, and customers to collaborate on remodel projects.  The [my.buildhero.io](https://my.buildhero.io) site, the Android app, and the iOS app are all powered by Butterfly Server .NET.

# Similar Projects

- [Cettia](https://cettia.io/)
- [dotNetify](https://github.com/dsuryd/dotNetify)
- [FeatherJS](https://feathersjs.com/)
- [Firehose](http://firehose.io/)
- [Meteor](https://www.meteor.com/)
- [PubNub](https://www.pubnub.com/)
- [Pusher](https://pusher.com/)
- [SignalR](https://github.com/SignalR/SignalR)
- [SignalW](https://github.com/Spreads/SignalW)

# Wishlist

Here is an unprioritized wish list going forward...

## More Databases

Add support for the following databases...

- Mongo DB

## More Client Bindings

Add support for the following clients...

- React
- React Native
- Angular
- UWP
- WinForms
- Android
- Flutter
- Swift

## More Examples

Add examples that show...

- Getting real-time data from a  *Raspberry Pi*

## Other Stuff

- Rework custom transactions to use System.Transactions namespace
- Splitout transport (WebSocket, long polling, etc) from subscription logic in Butterfly.Core.Channel
- Add performance benchmarks
- Add documentation for *Butterfly.Auth*
- Add documentation for *Butterfly.Notify*

If you'd like to contribute, please fork the repository and use a feature
branch. Pull requests are warmly welcome.

# Licensing

The code is licensed under the [Mozilla Public License 2.0](http://mozilla.org/MPL/2.0/).