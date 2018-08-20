# Butterfly Todo Server Example

> A simple Todo app built using Vue / Vuetify on the client


# Run this Example

```
git clone https://github.com/firesharkstudios/butterfly-server-dotnet.git

cd butterfly-server-dotnet\Butterfly.Example.Todo.Client
npm install

# Open Butterfly.sln in Visual Studio
# Run Butterfly.Example.Todo.Server in Visual Studio

npm run dev
```

You should see http://localhost:8080/ open in a browser. Try opening a second browser instance at http://localhost:8080/. Notice that changes are automatically synchronized between the two browser instances.

# What's Happening

1. Each client opens and maintains a WebSocket connection via the *WebSocketChannelClient* class
2. Each client subscribes to a *todos* channel that receives an initial list of records in the *todo* table and any changes to the *todo* table
1. Each client invokes API calls on the server to INSERT/DELETE records from a todo table
4. Data changes are automatically synchronized from the server to the client modifying local javascript arrays which automatically update the UI via the magic of *Vue*

See [Setup.cs](https://github.com/firesharkstudios/butterfly-server-dotnet/blob/master/Butterfly.Example.Todo.Server/Setup.cs) for the server code that sets up the database, handles API requests, and handles subscription requests.
