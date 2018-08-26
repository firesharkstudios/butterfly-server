# Butterfly Server .NET - Cordova Todo Client

> A simple Todo app built using Cordova / Vue / Vuetify on the client


# Run this Example

This assumes you have [Cordova](https://cordova.apache.org/) installed.

Run this in a terminal or command prompt...

```
git clone https://github.com/firesharkstudios/butterfly-server-dotnet

cd butterfly-server-dotnet\Butterfly.Example.Todo.Server
dotnet run -vm
```

Run this in a second terminal or command prompt...

```
cd butterfly-server-dotnet\Butterfly.Example.Todo.CordovaClient
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

If multiple clients are run, the *todos* will be automatically synchronized across all clients.

# See Also

- [Todo Server](https://github.com/firesharkstudios/butterfly-server-dotnet/tree/master/Butterfly.Example.Todo.Server)
- [Todo Web Client](https://github.com/firesharkstudios/butterfly-server-dotnet/tree/master/Butterfly.Example.Todo.Client)
- [Todo Electron Client](https://github.com/firesharkstudios/butterfly-server-dotnet/tree/master/Butterfly.Example.Todo.ElectronClient)

