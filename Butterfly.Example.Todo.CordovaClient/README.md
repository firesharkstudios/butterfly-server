# Butterfly Server .NET - Cordova Todo Client

> A simple Todo app built using Cordova / Vue / Vuetify on the client


# Run this Example

```
# This assumes you already have Cordova installed

git clone https://github.com/firesharkstudios/butterfly-server-dotnet

cd butterfly-server-dotnet\Butterfly.Example.Todo.CordovaClient
npm install

# In both config.xml and src\main.js, replace every instance of localhost:8000
# with <your DHCP assigned IP address>:8000 (like 192.168.1.15:8000)

npm run build
cordova platform add android

# Open Butterfly.sln in Visual Studio
# Run Butterfly.Example.Todo.Server in Visual Studio

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

