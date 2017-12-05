# Butterfly Framework
> Declarative framework for building real-time apps using C#

Build real-time web apps quickly using C# on the server and your favorite client libraries ([Vue.js](https://vuejs.org/), [AngularJS](https://angularjs.org/), [React](https://reactjs.org/)).  

On the server, declare the data to automatically synchronize with clients using a familiar SELECT syntax...

```csharp
// Listen for clients creating new channels to /hello-world,
// clients are expected to maintain a channel to the server,
// channels are currently implemented over WebSockets
channelServer.OnNewChannel("/hello-world", channel => {
    // When a channel is created, create a DynamicView on the message table sending all 
    // initial data and data changes to the client over the channel and returning the 
    // DynamicView so the DynamicView is disposed when the Channel is disposed
    return database.CreateAndStartDynamicView(
        "SELECT * FROM message",
        dataEventTransaction => {
            channel.Queue(dataEventTransaction);
        }
    );
});
```

On the web client, synchronize the data received into local arrays bound to UI elements by [Vue.js](https://vuejs.org/), [AngularJS](https://angularjs.org/), [React](https://reactjs.org/)...

```js
// Create channel to server and handle data events
// (mapping data events for the 'message' DynamicView to a local 'messages' array)
let channelClient = new WebSocketChannelClient({
    url: '/hello-world?id=' + myChannelId,
    onDataEvent: new ArrayDataEventHandler({
        arrayMapping: {
            message: messages,
        }
    })
});
channelClient.start();
```

## Getting Started

1. Clone the github repository `https://github.com/fireshark/Butterfly.git`
1. Open `Butterfly.sln` in Visual Studio 2017

## Examples

- [Hello World](http://examples.butterflyframework.io/examples/hello-world/index.html)

- [Minimal Chat (Using Vue.js)](http://examples.butterflyframework.io/examples/minimal-chat/index.vue.html)

- [Minimal Chat (Using AngularJS)](http://examples.butterflyframework.io/examples/minimal-chat/index.angular.html)

- [Full Chat (Using Vue.js)](http://examples.butterflyframework.io/examples/full-chat/index.vue.html)

## Packages

There are three key packages to the Butterfly Framework...

- [Butterfly.Database](https://firesharkstudios.github.io/Butterfly/Butterfly.Database) - Allows executing INSERT, UPDATE, and DELETE statements; creating dynamic views; and receiving data change events both on tables and dynamic views.  This is the bread and butter of the Butterfly Framework.
 
- [Butterfly.Channel](https://firesharkstudios.github.io/Butterfly/Butterfly.Channel) - Allows clients to create new channels to the server and allows the server to push messages to connected clients (think WebSockets).

- [Butterfly.WebApi](https://firesharkstudios.github.io/Butterfly/Butterfly.WebApi) - Allows receiving API requests via HTTP (inspired by Express JS) by wrapping existing C# web servers.

There are no interdependencies between the packages. Use any subset of the packages you wish.

## Recommened App Design

#### Client Side
- Use all static HTML, Javascript, and CSS files
- Perform actions by calling a RESTful Api on a `WebApiServer`
- Create and maintain a `Channel` to a `ChannelServer` to receive data
- Use whatever data binding library you wish ([Vue.js](https://vuejs.org/), [AngularJS](https://angularjs.org/), [React](https://reactjs.org/), etc)
- Use whatever client UI library you wish ([Bootstrap](https://getbootstrap.com/), [Foundation](https://foundation.zurb.com/), [Skeleton](http://getskeleton.com/), etc)

#### Server Side 
- Declare the data to automatically synchronize with clients using a familiar SELECT syntax
- Define the RESTful API of the `WebApiServer` using an Express JS like syntax
- Integrate with various .NET web servers (EmbedIO, RedHttpServer, etc)
- Integrate with popular databases (MySQL, Postgres, SQLite, etc)

## Environments

- Tested on .NET Framework 4.6.1, Windows 10 64 bit
- Tested on Mono 4.2.1, Ubuntu 16.04.3 LTS

## Next Steps

- Create NuGet packages for Butterfly.Database, Butterfly.Channel, and Butterfly.WebApi
- Add Butterfly.Database.MongoDB
- Add Butterfly.WebApi.NancyFX
- Add Butterfly.WebApi.Kestral
- Add example using React on client
- Add example running on Raspbian

## Contributing

If you'd like to contribute, please fork the repository and use a feature
branch. Pull requests are warmly welcome.

## Licensing

The core server code is licensed under the Apache License 2.0.  

All the example server code and client code is licensed under the MIT License.

