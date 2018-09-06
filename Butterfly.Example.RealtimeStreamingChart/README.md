# Real-time Streaming Chart Example as Web App

> A simple real-time streaming chart app built using Smoothie Charts on the client

# Demo

![Demo](https://raw.githubusercontent.com/firesharkstudios/butterfly-server-dotnet/master/img/real-time-chart-demo.gif) 

# Get the Code

```
git clone https://github.com/firesharkstudios/butterfly-server-dotnet
```

# Run It

To run in *Visual Studio*...
- Open *Butterfly.sln*
- Run *Butterfly.Example.RealtimeStreamingChart*.

To run in a terminal or command prompt...
```
cd butterfly-server-dotnet\Butterfly.Example.RealtimeStreamingChart
dotnet run -vm www
```

You can see the server code that runs at [Program.cs](https://github.com/firesharkstudios/butterfly-server-dotnet/blob/master/Butterfly.Example.RealtimeStreamingChart/Program.cs).

This will open a browser to http://localhost:8000/.

This is based on [Smoothie Charts Tutorial](http://smoothiecharts.org/tutorial.html) but changes the example to receive the real-time data from [Butterfly Server .NET](https://butterflyserver.io).

