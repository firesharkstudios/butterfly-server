# Butterfly Todo Example

> A simple Todo app built using Vue / Vuetify on the client

Before running this example...

1. In a terminal or command prompt, run `npm install` in Butterfly.Example.Todo.Client

Run this example...

1. In Visual Studio, run Butterfly.Example.Todo.Server
1. In a terminal or command prompt, run `npm run dev` in Butterfly.Example.Todo.Client
1. In a browser, open http://localhost:8000/
1. In a second browser, open http://localhost:8000/ again
 
Notice that both browser stay synchronized as you add/remove todos.

What's happening...

1. Each client opens and maintains a WebSocket connection via the *WebSocketChannelClient* class
2. Each client subscribes to a *todos* channel that receives an initial list of records in the *todo* table and any changes to the *todo* table
1. Each client invokes API calls on the server to INSERT/DELETE records from a todo table
4. Data changes are automatically synchronized from the server to the client modifying local javascript arrays which automatically update the UI via the magic of *Vue*

See https://github.com/firesharkstudios/Butterfly-Realtime-Web-App-Server/blob/master/Butterfly.Example.Todo.Server/Setup.cs for the server code that handles the API requests and pushes data changes to the clients.