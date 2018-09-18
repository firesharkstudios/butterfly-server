# Butterfly ASP.NET Core & Aurelia Demo

This demo is created using:  
* [ASP.NET Core 2.1](https://docs.microsoft.com/en-us/aspnet/core/?view=aspnetcore-2.1) Web API
  - [Butterfly Memory Database](https://butterflyserver.io/#/?id=using-a-memory-database)  
  - [WebSockets](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/websockets?view=aspnetcore-2.1)
* [Aurelia](https://aurelia.io/) Client  
  - [Butterfly Client](https://butterflyserver.io/#/?id=butterfly-client)


# Run the Server

To run in *Visual Studio*...
- Open *Butterfly.sln*
- Run *Butterfly.Example.AspNetCore*.

To run in a terminal or command prompt...
```
cd butterfly-server-dotnet\Butterfly.Example.AspNetCore
dotnet run -vm
```

# Run the Client  

This assumes you have the [demo server](#run-the-server) running and [Aurelia-CLI](https://aurelia.io/docs/build-systems/aurelia-cli/) installed: `npm install aurelia-cli -g`

```
cd butterfly-server-dotnet\Butterfly.Example.AspNetCore\aurelia
npm install
au run
```

Browse to http://localhost:8080

# Understanding The Server

## Configuration

The required services are registered as singletons using [ASP.NET Core DI](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-2.1)

```csharp
private void ConfigureButterflyServices(IServiceCollection services)
{
    var memoryDatabase = new MemoryDatabase();
    Task.WaitAll(memoryDatabase.CreateFromSqlAsync(@"CREATE TABLE todo (
            id VARCHAR(50) NOT NULL,
            name VARCHAR(40) NOT NULL,
            PRIMARY KEY(id)
        );"));
    memoryDatabase.SetDefaultValue("id", (tableName) =>
    {
        return $"{tableName}_{Guid.NewGuid().ToString()}";
    });

    // register for IoC
    services.AddSingleton(memoryDatabase);
    services.AddSingleton<WebSocketsSubscriptionApi>();
    services.AddSingleton<WebSocketsChannelConnection>();
}
```

The `WebSocketHandler` middleware is mapped to request on path: `"/ws"`

```csharp
public void Configure(IApplicationBuilder app, IHostingEnvironment env)
{
    ...

    app.Map("/ws", WebSocketsMiddleware.WebSocketHandler);			
}

public class WebSocketsMiddleware
{
    public static void WebSocketHandler(IApplicationBuilder app)
    {
        app.Run(async context =>
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = 400;
                return;
            }

            var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            var webSocketsChannelConnection = context.RequestServices.GetService<WebSocketsChannelConnection>();

            await webSocketsChannelConnection.ReceiveAsync(context, webSocket);
        });
    }
}
```

## Pub/Sub

The above middleware gets the registered `MemoryDatabase` and `WebSocketsChannelConnection` through IoC, which is then used to receive subscriptions and send update messages via `CreateAndStartDynamicViewAsync`
```csharp
public class WebSocketsChannelConnection : BaseChannelConnection
{
    public WebSocketsChannelConnection(MemoryDatabase memoryDatabase, WebSocketsSubscriptionApi subscriptionApi) : base(subscriptionApi)
    {
        _memoryDatabase = memoryDatabase;
    }

    protected override async Task SendAsync(string text)
    {
        ...
        await webSocket.SendAsync(...);
    }
    
    private readonly MemoryDatabase _memoryDatabase;

    public async Task ReceiveAsync(HttpContext context, WebSocket webSocket)
    {
        ...
        await _memoryDatabase.CreateAndStartDynamicViewAsync("todo", dataEventTransaction =>
        {
            _channel.Queue(dataEventTransaction);
        });
    }
}
```  

## Controller Endpoints  

The `TodosController` gets the `MemoryDatabase` singleton through IoC, which will publish events that's subscribed via the above `CreateAndStartDynamicViewAsync`

```csharp
[ApiController]
[Route("api/{constoller}")]
public class TodosController : ControllerBase
{
    private readonly MemoryDatabase _database;

    public TodosController(MemoryDatabase database)
    {
        _database = database;
    }
    
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] Todo todo)
    {
        var result = await _database.InsertAndCommitAsync<string>("todo", todo);
        return Ok(result);
    }
    
    [HttpDelete]
    public async Task<IActionResult> Delete([FromBody] string id)
    {
        await _database.DeleteAndCommitAsync("todo", id);
        return NoContent();
    }
}
```

# Understanding The Client

The `Todo` class connects and subscribes to the `"todo"` websocket channel, to receive and display any data changes in real time.

```js
import { ArrayDataEventHandler, WebSocketChannelClient } from 'butterfly-client';

export class Todo {
  constructor() {
    this.todoList = [];
    this.channel = 'todos';
    this.channelClient = null;
    this.channelClientState = null;
    this.connect();
    this.subscribe();
  }
  
  connect() {
    let url = 'ws://localhost:8000/ws';
    this.channelClient = new WebSocketChannelClient({
      url,
      onStateChange(value) {
        this.channelClientState = value;
      }
    });

    this.channelClient.connect();
  }

  subscribe() {
    this.channelClient.subscribe({
      channel: this.channel,
      vars: {
        clientName: 'AureliaWebClient'
      },
      handler: new ArrayDataEventHandler({
        arrayMapping: {
          todo: this.todoList
        }
      })
    });
  }
}
```

Standard [Fetch API](https://developer.mozilla.org/en-US/docs/Web/API/Fetch_API) methods are used to update data via the controller endpoints.

```js
// app.js
addTodo() {
    return fetch('http://localhost:8000/api/todo', {
      method: 'POST',
      headers:{
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({ name: this.todoName })
    });
}

// todo.js
removeTodo(todo) {
    return fetch('http://localhost:8000/api/todo', {
        method: 'DELETE',
        headers:{
        'Content-Type': 'application/json'
        },
        body: JSON.stringify(todo.id)
    });
}
```

___
> See [Butterfly.Example.Todo](https://github.com/firesharkstudios/butterfly-server-dotnet/tree/master/Butterfly.Example.Todo/vue) for a full working client based on [Vuetify](https://vuetifyjs.com) and [Vue.js](https://vuejs.org/).
