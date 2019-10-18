# Echo Example

> A simple echo web client and server using [Butterfly.Clients](https://github.com/firesharkstudios/butterfly-client), [Butterfly.Web](https://github.com/firesharkstudios/butterfly-web), and [Butterfly.Db](https://github.com/firesharkstudios/butterfly-db)

This example shows...

- A plain HTML/JS client subscribing to the Subscription API via the [Butterfly.Clients](https://github.com/firesharkstudios/butterfly-client) web client to receive updates on the "messages" channel and echoing any updates in an alert() box
- A plain HTML/JS web client invoking a Web API method to insert a "Hello World" message
- Implementing the Web API from [Butterfly.Web](https://github.com/firesharkstudios/butterfly-web) on the server
- Implementing the Subscription API from [Butterfly.Web](https://github.com/firesharkstudios/butterfly-web) on the server
- Using [Butterfly.Db](https://github.com/firesharkstudios/butterfly-db) on the server to perform database operations and publish the data change events to subscribers

This example uses a plain HTML/JS client but the [Butterfly.Clients](https://github.com/firesharkstudios/butterfly-client) web client is vanilla javascript and can be used with any framework.

This example uses the [EmbedIO](https://github.com/unosquare/embedio) web server but [Butterfly.Web](https://github.com/firesharkstudios/butterfly-web) supports multiple web servers.

This example uses a memory database but [Butterfly.Db](https://github.com/firesharkstudios/butterfly-db) supports multiple database engines.

# Get the Code

```
git clone https://github.com/firesharkstudios/butterfly-server
```

# Running

Open a terminal or command prompt and run...

```
cd butterfly-server\Butterfly.Example.Echo\www
npm install
cd ..
dotnet run www
```

Open a browser to http://localhost:8000/ and you should see "Hello World" shown in an alert box.
