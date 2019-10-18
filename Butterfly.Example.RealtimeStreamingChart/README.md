# Real-time Streaming Chart Example

> A simple real-time streaming chart using Smoothie Charts, [Butterfly.Clients](https://github.com/firesharkstudios/butterfly-client), [Butterfly.Web](https://github.com/firesharkstudios/butterfly-web), and [Butterfly.Db](https://github.com/firesharkstudios/butterfly-db)

# Demo

![Demo](https://raw.githubusercontent.com/firesharkstudios/butterfly-server/master/img/real-time-chart-demo.gif) 

# Get the Code

```
git clone https://github.com/firesharkstudios/butterfly-server
```

# Run It

```
cd butterfly-server\Butterfly.Example.RealtimeStreamingChart
dotnet run www
```

Now open a browser to http://localhost:8000/.

You can see the server code that runs at [Program.cs](https://github.com/firesharkstudios/butterfly-server/blob/master/Butterfly.Example.RealtimeStreamingChart/Program.cs).

This is based on [Smoothie Charts Tutorial](http://smoothiecharts.org/tutorial.html) but changes the example to receive the real-time data from [Butterfly Server](https://butterflyserver.io).

