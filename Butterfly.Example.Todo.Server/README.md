# Butterfly Server .NET - Todo Server

> A simple Todo app built using Vue / Vuetify on the client


# Run this Example

This example is available with multiple clients...

- [Web Client](https://github.com/firesharkstudios/butterfly-server-dotnet/tree/master/Butterfly.Example.Todo.Client)
- [Cordova Client](https://github.com/firesharkstudios/butterfly-server-dotnet/tree/master/Butterfly.Example.Todo.CordovaClient)
- [Electron Client](https://github.com/firesharkstudios/butterfly-server-dotnet/tree/master/Butterfly.Example.Todo.ElectronClient)

Click any link above to follow the instructions specific to that client.

# What's Happening

1. Each client opens and maintains a WebSocket connection via the *WebSocketChannelClient* class
2. Each client subscribes to a *todos* channel that receives an initial list of records in the *todo* table and any changes to the *todo* table
1. Each client invokes API calls on the server to INSERT/DELETE records from a todo table
4. Data changes are automatically synchronized from the server to the client modifying local javascript arrays which automatically update the UI via the magic of *Vue*

See [Setup.cs](https://github.com/firesharkstudios/butterfly-server-dotnet/blob/master/Butterfly.Example.Todo.Server/Setup.cs) for the server code that sets up the database, handles API requests, and handles subscription requests.