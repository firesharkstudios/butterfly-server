# Butterfly Server .NET - Database World Example

> A simple console app that triggers data change events on a DynamicView

# Get the Code

```
git clone https://github.com/firesharkstudios/butterfly-server-dotnet
```

# Run

To run in *Visual Studio*...
- Open *Butterfly.sln*
- Run *Butterfly.Example.Database*.

To run in a terminal or command prompt...
```
cd butterfly-server-dotnet\Butterfly.Example.Database
dotnet run -vm
```

You can see the code that runs at [Program.cs](https://github.com/firesharkstudios/butterfly-server-dotnet/blob/master/Butterfly.Example.Database/Program.cs).

# Result

Here is the expected console output...

```
Creating sample data...
dataEvents={
  "dateTime": "2018-09-03 09:54:15",
  "dataEvents": [
    {
      "name": "todo",
      "keyFieldNames": [
        "id"
      ],
      "dataEventType": "InitialBegin",
      "id": "7170c6d5-892d-4396-a5f7-4308d2e615a7"
    },
    {
      "record": {
        "id": "t_62fa54f4-8f37-44fe-83ca-26345b3a684d",
        "todo_name": "Todo #4",
        "user_name": "Patrick"
      },
      "name": "todo",
      "keyValue": "t_62fa54f4-8f37-44fe-83ca-26345b3a684d",
      "dataEventType": "Initial",
      "id": "e5e14381-bb69-4d29-be90-8baefc44b0a3"
    },
    {
      "record": {
        "id": "t_5ad42a75-0b4e-441f-b73a-3b847f8eff6f",
        "todo_name": "Todo #1",
        "user_name": "Spongebob"
      },
      "name": "todo",
      "keyValue": "t_5ad42a75-0b4e-441f-b73a-3b847f8eff6f",
      "dataEventType": "Initial",
      "id": "e50b1ba2-810f-4f10-a7ce-aa5be9c62b1e"
    },
    {
      "record": {
        "id": "t_eef5aec9-0fce-4fd4-96bb-544a073dad1d",
        "todo_name": "Todo #2",
        "user_name": "Spongebob"
      },
      "name": "todo",
      "keyValue": "t_eef5aec9-0fce-4fd4-96bb-544a073dad1d",
      "dataEventType": "Initial",
      "id": "7088727d-1801-4961-a4ab-58e273914c3a"
    },
    {
      "dataEventType": "InitialEnd",
      "id": "34cbeadc-e072-458f-8251-f92c21a3f6b6"
    }
  ]
}
Inserting Task #5...
dataEvents={
  "dateTime": "2018-09-03 09:54:15",
  "dataEvents": [
    {
      "record": {
        "id": "t_fbf5fabc-5896-4b01-bbd3-f991e0d9057b",
        "todo_name": "Task #5",
        "user_name": "Spongebob"
      },
      "name": "todo",
      "keyValue": "t_fbf5fabc-5896-4b01-bbd3-f991e0d9057b",
      "dataEventType": "Insert",
      "id": "c4fa59be-a477-48c7-82fc-7af82c5349da"
    }
  ]
}
Inserting Task #6...
Updating task name to 'Updated Task #1'...
dataEvents={
  "dateTime": "2018-09-03 09:54:16",
  "dataEvents": [
    {
      "record": {
        "id": "t_5ad42a75-0b4e-441f-b73a-3b847f8eff6f",
        "todo_name": "Updated Task #1",
        "user_name": "Spongebob"
      },
      "name": "todo",
      "keyValue": "t_5ad42a75-0b4e-441f-b73a-3b847f8eff6f",
      "dataEventType": "Update",
      "id": "08a0b6b4-d8e8-4fbf-9bbe-8605da11c7a3"
    }
  ]
}
Updating user name to 'Mr. Spongebob'
dataEvents={
  "dateTime": "2018-09-03 09:54:17",
  "dataEvents": [
    {
      "record": {
        "id": "t_5ad42a75-0b4e-441f-b73a-3b847f8eff6f",
        "todo_name": "Updated Task #1",
        "user_name": "Mr. Spongebob"
      },
      "name": "todo",
      "keyValue": "t_5ad42a75-0b4e-441f-b73a-3b847f8eff6f",
      "dataEventType": "Update",
      "id": "c8f6b0ac-f429-41bf-b95a-cdb388e1a732"
    },
    {
      "record": {
        "id": "t_eef5aec9-0fce-4fd4-96bb-544a073dad1d",
        "todo_name": "Todo #2",
        "user_name": "Mr. Spongebob"
      },
      "name": "todo",
      "keyValue": "t_eef5aec9-0fce-4fd4-96bb-544a073dad1d",
      "dataEventType": "Update",
      "id": "60a947c7-06d7-45ea-ac91-9ebb2b2ea381"
    },
    {
      "record": {
        "id": "t_fbf5fabc-5896-4b01-bbd3-f991e0d9057b",
        "todo_name": "Task #5",
        "user_name": "Mr. Spongebob"
      },
      "name": "todo",
      "keyValue": "t_fbf5fabc-5896-4b01-bbd3-f991e0d9057b",
      "dataEventType": "Update",
      "id": "6d10ce3f-668c-4f97-9ddc-efc96baa7cc0"
    }
  ]
}
```