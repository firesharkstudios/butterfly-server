# CRUD Example

> A simple contact manager app using Vue.js

# Demo

![Demo](https://raw.githubusercontent.com/firesharkstudios/butterfly-server-dotnet/master/img/real-time-chart-demo.gif) 

# Get the Code

```
git clone https://github.com/firesharkstudios/butterfly-server-dotnet
```

# Run the Server

To run in *Visual Studio*...
- Open *Butterfly.sln*
- Run *Butterfly.Example.Crud*.

To run in a terminal or command prompt...
```
cd butterfly-server-dotnet\Butterfly.Example.Crud
dotnet run -vm
```

You can see the server code that runs at [Program.cs](https://github.com/firesharkstudios/butterfly-server-dotnet/blob/master/Butterfly.Example.Crud/Program.cs).

## Run the Client

This assumes you have the [demo server](#run-the-server) running.

```
cd butterfly-server-dotnet\Butterfly.Example.Crud\www
npm install
npm run dev
```

This will open a browser to http://localhost:8080/.

Now, open as many other browser instances to http://localhost:8080/ as you wish to confirm the todo list stays synchronized across all connected clients.

**Note:** The server process is listening for API requests on port 8000 and the node dev server is listening on port 8080 and proxying API requests to port 8000.
