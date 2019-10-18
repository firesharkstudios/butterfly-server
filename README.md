# *Butterfly.Server* ![Butterfly Logo](https://raw.githubusercontent.com/firesharkstudios/butterfly-server/master/img/logo-40x40.png) 

> The Everything is Real-Time C# Backend for Single Page Applications

# Overview

*Butterfly.Server* is a set of packages that can be used separately
or can be used together to create integrated solutions to create 
modern web apps with a C# backend. The purpose of this repo is to show examples of how to use the components together.

*Butterfly.Server* is especially well suited to powering SPAs (single page applications) by providing...

- A *RESTlike API* enabling clients to receive static data and perform actions
- A *Subscription API* enabling clients to automatically receive real-time updates when data changes
- A *Database API* that avoids object-relational mappings and publishes data change events
- A *Messaging API* that provides a consistent API to send emails and texts
 
*Butterfly.Server* consists of the following components...

- [Butterfly.Auth](https://github.com/firesharkstudios/butterfly-auth) - Authenticate clients using [Butterfly.Db](https://github.com/firesharkstudios/butterfly-db) and [Butterfly.Web](https://github.com/firesharkstudios/butterfly-web)
- [Butterfly.Client](https://github.com/firesharkstudios/butterfly-client) - Clients (javascript and .NET) that can subscribe real-time updates from a Butterfly.Web server
- [Butterfly.Db](https://github.com/firesharkstudios/butterfly-db) - Access a database without an ORM and subscribe to database change events
- [Butterfly.Message](https://github.com/firesharkstudios/butterfly-message) - Send emails and text messages via the same API
- [Butterfly.Util](https://github.com/firesharkstudios/butterfly-util) - Collection of utility classes and methods
- [Butterfly.Web](https://github.com/firesharkstudios/butterfly-web) - Simple RESTlike and Subscription API server

*Butterfly.Server*...

- Targets *.NET Framework 2.1* (.NET Core 3.0)
- Fully supports async/await
- Does **not** depend on ASP.NET
- Does **not** use polling for real-time updates

![Star Us](https://raw.githubusercontent.com/firesharkstudios/butterfly-server/master/img/yellow-star-16x16.png) Please star this project if you find it interesting

# Examples

The following examples are a good way to understand how to use the *Butterfly.Server* components...

- [Butterfly.Example.Contacts](https://github.com/firesharkstudios/butterfly-server/tree/master/Butterfly.Example.Contacts) - Shows a Vue.js web client managing a set of contacts by interacting with a *Butterfly.Server*
- [Butterfly.Example.DbEvents](https://github.com/firesharkstudios/butterfly-server/tree/master/Butterfly.Example.DbEvents) - Shows how to perform basic database operations and see generated data events
- [Butterfly.Example.Echo](https://github.com/firesharkstudios/butterfly-server/tree/master/Butterfly.Example.Echo) - Shows a "Hello World" alert box in a client
- [Butterfly.Example.RealtimeStreamingChart](https://github.com/firesharkstudios/butterfly-server/tree/master/Butterfly.Example.RealtimeStreamingChart) - Shows a client with a real-time streaming chart updated from a server (uses [Smoothie Charts](http://smoothiecharts.org/))
- [Butterfly.Example.Todos](https://github.com/firesharkstudios/butterfly-server/tree/master/Butterfly.Example.Todos) - Shows a Vue.js web client managing a set of to-dos by interacting with a *Butterfly.Server*

# Install from Nuget

| Name | Package | Install |
| --- | --- | --- |
| Butterfly.Auth | [![nuget](https://img.shields.io/nuget/v/Butterfly.Auth.svg)](https://www.nuget.org/packages/Butterfly.Auth/) | `nuget install Butterfly.Auth` |
| Butterfly.Db | [![nuget](https://img.shields.io/nuget/v/Butterfly.Db.svg)](https://www.nuget.org/packages/Butterfly.Db/) | `nuget install Butterfly.Db` |
| Butterfly.Db.MySql | [![nuget](https://img.shields.io/nuget/v/Butterfly.Db.MySql.svg)](https://www.nuget.org/packages/Butterfly.Db.MySql/) | `nuget install Butterfly.Db.MySql` |
| Butterfly.Db.Postgres | [![nuget](https://img.shields.io/nuget/v/Butterfly.Db.Postgres.svg)](https://www.nuget.org/packages/Butterfly.Db.Postgres/) | `nuget install Butterfly.Db.Postgres` |
| Butterfly.Db.SQLite | [![nuget](https://img.shields.io/nuget/v/Butterfly.Db.SQLite.svg)](https://www.nuget.org/packages/Butterfly.Db.SQLite/) | `nuget install Butterfly.Db.SQLite` |
| Butterfly.Db.SqlServer | [![nuget](https://img.shields.io/nuget/v/Butterfly.Db.SqlServer.svg)](https://www.nuget.org/packages/Butterfly.Db.SqlServer/) | `nuget install Butterfly.Db.SqlServer` |
| Butterfly.Web | [![nuget](https://img.shields.io/nuget/v/Butterfly.Web.svg)](https://www.nuget.org/packages/Butterfly.Web/) | `nuget install Butterfly.Web` |
| Butterfly.Web.EmbedIO | [![nuget](https://img.shields.io/nuget/v/Butterfly.Web.EmbedIO.svg)](https://www.nuget.org/packages/Butterfly.Web.EmbedIO/) | `nuget install Butterfly.Web.EmbedIO` |
| Butterfly.Web.RedHttpServer | [![nuget](https://img.shields.io/nuget/v/Butterfly.Web.RedHttpServer.svg)](https://www.nuget.org/packages/Butterfly.Web.RedHttpServer/) | `nuget install Butterfly.Web.RedHttpServer` |
| Butterfly.Util | [![nuget](https://img.shields.io/nuget/v/Butterfly.Util.svg)](https://www.nuget.org/packages/Butterfly.Util/) | `nuget install Butterfly.Util` |

# Install from Source Code

Get the source from these repos...

- [Butterfly.Auth](https://github.com/firesharkstudios/butterfly-auth)
- [Butterfly.Client](https://github.com/firesharkstudios/butterfly-db)
- [Butterfly.Db](https://github.com/firesharkstudios/butterfly-db)
- [Butterfly.Util](https://github.com/firesharkstudios/butterfly-util)
- [Butterfly.Web](https://github.com/firesharkstudios/butterfly-web)

# In the Wild

[Build Hero](https://www.buildhero.io) is a collaborative tool for general contractors, subcontractors, and customers to collaborate on remodel projects.  The [my.buildhero.io](https://my.buildhero.io) site and the Android app are all powered by *Butterfly.Server*.

[MotionAlerts.ai](https://www.motionalerts.ai) is a tool that uses AI to filter security camera alerts..  The [my.motionalerts.ai](https://my.motionalerts.ai) site and the Android app are all powered by *Butterfly.Server*.

Notice in the production apps above how everything updates in real-time across all connected clients when changes happen.

# Similar Projects

- [dotNetify](https://github.com/dsuryd/dotNetify)
- [FeatherJS](https://feathersjs.com/)
- [Firehose](http://firehose.io/)
- [Meteor](https://www.meteor.com/)
- [PubNub](https://www.pubnub.com/)
- [Pusher](https://pusher.com/)
- [SignalR](https://github.com/SignalR/SignalR)
- [SignalW](https://github.com/Spreads/SignalW)

# Licensing

The code is licensed under the [Mozilla Public License 2.0](http://mozilla.org/MPL/2.0/).
