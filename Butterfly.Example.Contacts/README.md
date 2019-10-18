# Contact Manager Example

> A simple contact manager app using Vue.js, butterfly-client, and Butterfly.Server

Th isexample shows...

- A Vue.js client calling a Web API to add, update, and delete contacts
- A Vue.js client using the *butterfly-client* library to receive updates when any data changes
- A *Butterfly Server* implementing the Web API and Subscription API

Although this example uses Vue.js, the *butterfly-client* is vanilla javascript and can be used with any framework.

See this [article](https://medium.com/@kent_19698/creating-a-basic-crud-web-app-with-vue-vuetify-and-butterfly-server-net-e35922138d4f) about building this example web app.

# Get the Code

```
git clone https://github.com/firesharkstudios/butterfly-server
```

# Run the Server

```
cd butterfly-server\Butterfly.Example.Contacts
dotnet run
```

You can see the server code that runs at [Program.cs](https://github.com/firesharkstudios/butterfly-server/blob/master/Butterfly.Example.Contacts/Program.cs).

# Run the Client

```
cd butterfly-server\Butterfly.Example.Contacts\www
npm install
npm run serve
```

Open as many browser instances to http://localhost:8080/ as you wish to confirm the todo list stays synchronized across all connected clients.
