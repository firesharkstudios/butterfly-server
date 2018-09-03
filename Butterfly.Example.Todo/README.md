# Butterfly Server .NET - Todo Example

> A simple Todo app built using Vue / Vuetify on the client

# Get the Code

```
git clone https://github.com/firesharkstudios/butterfly-server-dotnet
```

# Run the Server

To run in *Visual Studio*...
- Open *Butterfly.sln*
- Run *Butterfly.Example.Todo*.

To run in a terminal or command prompt...
```
cd butterfly-server-dotnet\Butterfly.Example.HelloWorld
dotnet run -vm
```

You can see the server code that runs at [Program.cs](https://github.com/firesharkstudios/butterfly-server-dotnet/blob/master/Butterfly.Example.Todo/Program.cs).

# Run the Web Client

```
cd butterfly-server-dotnet\Butterfly.Example.Todo\www
npm install
npm run dev
```

This will open a browser to http://localhost:8080/.

Now, open as many other browser instances to http://localhost:8080/ as you wish to confirm the todo list stays synchronized across all connected clients.

**Note:** The server process is listening for API requests on port 8000 and the node dev server is listening on port 8080 and proxying API requests to port 8000.

# Run the Cordova Client

This assumes you have [Cordova](https://cordova.apache.org/) and [Android Studio](https://developer.android.com/studio/) installed.

Run this in a terminal or command prompt...

```
cd butterfly-server-dotnet\Butterfly.Example.Todo\cordova
npm install

# In both config.xml and src\main.js, replace every instance of localhost:8000
# with <your DHCP assigned IP address>:8000 (like 192.168.1.15:8000)

npm run build
cordova platform add android

# Open Android Studio
# Click Tools, AVD Manager
# Startup the desired Android emulator

cordova run android
```

# Run the Electron Client

This assumes you have [Electron](https://electronjs.org/) installed.

Run this in a terminal or command prompt...

```
cd butterfly-server-dotnet\Butterfly.Example.Todo\electron
npm install
npm run dev
```
