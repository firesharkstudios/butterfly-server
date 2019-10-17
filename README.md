# Butterfly Server .NET ![Butterfly Logo](https://raw.githubusercontent.com/firesharkstudios/butterfly-server-dotnet/master/img/logo-40x40.png) 

> The Everything is Real-Time C# Backend for Web and Desktop Apps

![Demo](https://raw.githubusercontent.com/firesharkstudios/butterfly-server-dotnet/master/img/simple-chat-demo.gif) 

![Star Us](https://raw.githubusercontent.com/firesharkstudios/butterfly-server-dotnet/master/img/yellow-star-16x16.png) Please star this project if you find it interesting

Targets *.NET Standard 2.1* (.NET Core 3.0)

# Examples...

- [Real-time Streaming Charts](https://github.com/firesharkstudios/butterfly-server-dotnet/tree/master/Butterfly.Example.RealtimeStreamingChart) - Shows a client with a real-time streaming chart updated from a server (uses [Smoothie Charts](http://smoothiecharts.org/))
- [Contact Manager](https://github.com/firesharkstudios/butterfly-server-dotnet/tree/master/Butterfly.Example.Crud) - Shows basic CRUD operations where all changes synchronized to connected clients
- [Todo List Manager](https://github.com/firesharkstudios/butterfly-server-dotnet/tree/master/Butterfly.Example.Todo) - Shows different types of clients synchronized to the same server (a [Vue.js](https://vuejs.org/) client, a [Cordova](https://cordova.apache.org/) client, an [Electron](https://electronjs.org/) client, and an [Aurelia](https://aurelia.io/) client)
- [Hello World](https://github.com/firesharkstudios/butterfly-server-dotnet/tree/master/Butterfly.Example.HelloWorld) - Shows a "Hello World" alert box in a client

# Key Components...

*Butterfly.Server* is a set of packages that can be used separately or can be used together to create an integrated solutions to create modern web apps with a C# backend.  Here are the key packages...

- [Butterfly.Auth](https://github.com/firesharkstudios/butterfly-auth) - Authenticate clients in C# using Butterfly.Db and Butterfly.Web
- [Butterfly.Client](https://github.com/firesharkstudios/butterfly-client) - Clients (javascript and .NET) that can subscribe real-time updates from a Butterfly.Web server in C#
- [Butterfly.Db](https://github.com/firesharkstudios/butterfly-db) - Access a database without an ORM and subscribe to database change events in C#
- [Butterfly.Message](https://github.com/firesharkstudios/butterfly-message) - Send emails and text messages via the same API in C#
- [Butterfly.Util](https://github.com/firesharkstudios/butterfly-util) - Collection of utility methods used in the Butterfly Server
- [Butterfly.Web](https://github.com/firesharkstudios/butterfly-web) - Simple RESTlike and Subscription API server in C#

