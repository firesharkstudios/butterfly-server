# Butterfly Server .NET ![Butterfly Logo](https://raw.githubusercontent.com/firesharkstudios/butterfly-server-dotnet/master/img/logo-40x40.png) 

> The Everything is Real-Time C# Backend for Single Page Applications

![Demo](https://raw.githubusercontent.com/firesharkstudios/butterfly-server-dotnet/master/img/demo.gif) 

Butterfly Server .NET provides...

- A channel layer that allows the server to push real-time data to clients
- A database layer that performs standard database operations **and** allows subscribing to change events
- A web API layer that provides a simple syntax to define RESTlike APIs

Each layer can have multiple implementations...

- The channel layer might use WebSockets, long polling, etc
- The database layer might use a memory database, MySQL, SQLite, etc
- The web API layer might use EmbedIO, NancyFX, Kestrel, etc

Butterfly Server .NET does **not** have any dependencies on ASP.NET.

See https://firesharkstudios.github.io/butterfly-server-dotnet/ for more details.