# Butterfly Framework
> Declarative framework for building real-time apps using C#

A fast way to build real-time web apps using C# on the server and still use your favorite client libraries ([Vue.js](https://vuejs.org/), [AngularJS](https://angularjs.org/), [React](https://reactjs.org/)).  

Declare the server data to synchronize with clients using a familiar SELECT syntax and have the data automatically synchronized to clients.

## Getting Started

1. Clone the github repository `https://github.com/fireshark/butterfly-framework.git`
1. Open `Butterfly.sln` in Visual Studio 2017

## Examples

- [Hello World](http://examples.butterflyframework.io/examples/hello-world/index.html)

- [Minimal Chat (Using Vue.js)](http://examples.butterflyframework.io/examples/minimal-chat/index.vue.html)

- [Minimal Chat (Using AngularJS)](http://examples.butterflyframework.io/examples/minimal-chat/index.angular.html)

- [Full Chat (Using Vue.js)](http://examples.butterflyframework.io/examples/full-chat/index.vue.html)

## Packages

There are three key packages to the Butterfly Framework...

- [Butterfly.Database](https://firesharkstudios.github.io/Butterfly/Butterfly.Database) - Allows executing INSERT, UPDATE, and DELETE statements; creating dynamic views; and receiving data change events both on tables and dynamic views.  This is the bread and butter of the Butterfly Framework.
 
- [Butterfly.Channel](https://firesharkstudios.github.io/Butterfly/Butterfly.Channel) - Allows clients to create new channels to the server and allows the server to push messages to connected clients.

- [Butterfly.WebApi](https://firesharkstudios.github.io/Butterfly/Butterfly.WebApi) - Allows receiving API requests via HTTP (inspired by Express JS) by wrapping existing C# web servers.

There are no interdependencies between the packages. Use any subset of the packages you wish.

## App Design

#### Client Side
- Use all static HTML, Javascript, and CSS files (no templating needed)
- Perform actions by calling a RESTful Api on a `WebApiServer`
- Create and maintain a `Channel` to a `ChannelServer` to receive data
- Integrate with popular data binding libraries (Vue.js, Angular, React, etc) to auto update HTML as data changes

#### Server Side 
- Define the synchronized data each client receives by creating `DynamicViews` on a `Database` using a familiar SELECT syntax
- Define the RESTful API of the `WebApiServer` using an Express JS like syntax
- Integrate with various .NET web servers (EmbedIO, RedHttpServer, etc)
- Integrate with popular databases (MySQL, Postgres, SQLite, etc)

## Status

Alpha but actively implementing features and test cases.

## Contributing

If you'd like to contribute, please fork the repository and use a feature
branch. Pull requests are warmly welcome.

## Licensing

The core server code is licensed under the Apache License 2.0.  

All the example server code and client code is licensed under the MIT License.

