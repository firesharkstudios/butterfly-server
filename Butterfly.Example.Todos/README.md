# To-Do Manager Example

> A simple to-do manager web client and server using Vue.js, [Butterfly.Clients](https://github.com/firesharkstudios/butterfly-client), [Butterfly.Web](https://github.com/firesharkstudios/butterfly-web), and [Butterfly.Db](https://github.com/firesharkstudios/butterfly-db)

This example shows...

- A Vue.js client invoking Web API methods to add and delete to-dos
- A Vue.js client subscribing to the Subscription API via the [Butterfly.Clients](https://github.com/firesharkstudios/butterfly-client) to receive updates when any data changes
- Implementing the Web API from [Butterfly.Web](https://github.com/firesharkstudios/butterfly-web) on the server
- Implementing the Subscription API from [Butterfly.Web](https://github.com/firesharkstudios/butterfly-web) on the server
- Using [Butterfly.Db](https://github.com/firesharkstudios/butterfly-db) on the server to perform database operations and publish the data change events to subscribers

This example uses Vue.js but the [Butterfly.Clients](https://github.com/firesharkstudios/butterfly-client) web client is vanilla javascript and can be used with any framework.

This example uses the [EmbedIO](https://github.com/unosquare/embedio) web server but [Butterfly.Web](https://github.com/firesharkstudios/butterfly-web) supports multiple web servers.

This example uses the memory database but [Butterfly.Db](https://github.com/firesharkstudios/butterfly-db) supports multiple database engines.

# Get the Code

```
git clone https://github.com/firesharkstudios/butterfly-server
```

# Run the Server

```
cd butterfly-server\Butterfly.Example.Todos
dotnet run
```

You can see the server code that runs at [Program.cs](https://github.com/firesharkstudios/butterfly-server/blob/master/Butterfly.Example.Todos/Program.cs).

## Run the Client

```
cd butterfly-server\Butterfly.Example.Todos\www
npm install
npm run serve
```

Now, open as many browser instances to http://localhost:8080/ as you wish to confirm the todo list stays synchronized across all connected clients.

# Understanding The Server

Here is all server code for our todo list manager...

```csharp
using System;

using Butterfly.Core.Util;

using Dict = System.Collections.Generic.Dictionary<string, object>;

namespace Butterfly.Example.Todos {
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

See [Program.cs](https://github.com/firesharkstudios/butterfly-server/tree/master/Butterfly.Example.Todos/Program.cs) for the working server code.

# Understanding The Client

Now, let's see how a client might interact with this server using the [Butterfly Client](#butterfly-client) javascript library.

First, the client should use *WebSocketChannelClient* to maintain an open WebSocket to the server...

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

See [Butterfly.Example.Todos](https://github.com/firesharkstudios/butterfly-server/tree/master/Butterfly.Example.Todos/vue) for a full working client based on [Vuetify](https://vuetifyjs.com) and [Vue.js](https://vuejs.org/).
