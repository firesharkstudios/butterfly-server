# Butterfly Framework
> Declarative framework for building real-time apps using C#

Create real-time web apps with minimal coding.  Declare the data that should be synchronized with each client using familiar SELECT syntax. Let the Butterfly Framework send the initial data and any changes to every client.

## Recommended App Architecture

Client side...

- Static HTML, CSS, and Javascript files
- Use `butterfly.channel.js` to receive data from the server (WebSockets, long polling, etc)
- Use `butterfly.data.js` to map the data to your favorite data binding library (Vue, Angular, React, etc)
- Call your REST API to make data changes and to perform actions

Server side...

- Use `Butterfly.WebServer` and `Butterfly.Channel` namespaces to use your favorite HTTP server (IIS, Kestral, Nancy FX, Red Web Server, etc)
- Use `Butterfly.Database` namespace to use your favorite database (MySQL, Postgres, SQLite, etc) 
- Implement your REST API using `webServer.OnGet`, `webServer.OnPost`, etc.
- Send data to clients using `channelServer.Send`
- Listen for database changes using `database.OnNewCommittedTransaction`

## Getting Started

1. Clone the github repository `https://github.com/fireshark/butterfly-framework.git`
1. Open `Butterfly.sln` in Visual Studio 2017

## Examples

See the Wiki for examples

## Contributing

If you'd like to contribute, please fork the repository and use a feature
branch. Pull requests are warmly welcome.

## Licensing

Everything is licensed under the Apache License 2.0.

