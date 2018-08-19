# MemoryDatabase class

Allows executing SELECT statements, creating transactions to execute INSERT, UPDATE, and DELETE statements; creating dynamic views; and receiving data change events both on tables and dynamic views.

Allows executing SELECT statements, creating transactions to execute INSERT, UPDATE, and DELETE statements; creating dynamic views; and receiving data change events both on tables and dynamic views.

Allows executing SELECT statements, creating transactions to execute INSERT, UPDATE, and DELETE statements; creating dynamic views; and receiving data change events both on tables and dynamic views.

Allows executing SELECT statements, creating transactions to execute INSERT, UPDATE, and DELETE statements; creating dynamic views; and receiving data change events both on tables and dynamic views.

Allows executing SELECT statements, creating transactions to execute INSERT, UPDATE, and DELETE statements; creating dynamic views; and receiving data change events both on tables and dynamic views.

Allows executing SELECT statements, creating transactions to execute INSERT, UPDATE, and DELETE statements; creating dynamic views; and receiving data change events both on tables and dynamic views.

Allows executing SELECT statements, creating transactions to execute INSERT, UPDATE, and DELETE statements; creating dynamic views; and receiving data change events both on tables and dynamic views.

```csharp
public class MemoryDatabase : BaseDatabase
```

## Public Members

| name | description |
| --- | --- |
| [MemoryDatabase](MemoryDatabase/MemoryDatabase.md)() | The default constructor. |
| override [CanFieldAlias](MemoryDatabase/CanFieldAlias.md) { get; } |  |
| override [CanJoin](MemoryDatabase/CanJoin.md) { get; } |  |
| static [EvaluateWhereClause](MemoryDatabase/EvaluateWhereClause.md)(…) |  |

## Remarks

Adding records and echoing all data change events to the console...

```csharp
// Create database instance (will also read the schema from the database)
var database = new SomeDatabase();

// Listen for all database data events
var databaseListener = database.OnNewCommittedTransaction(dataEventTransaction => {
    console.WriteLine($"Low Level DataEventTransaction={dataEventTransaction}");
});

// INSERT a couple of records (this will cause a single data even transaction with
// two INSERT data events to be written to the console above)
using (var transaction = database.BeginTransaction()) {
    await database.InsertAndCommitAsync("employee", values: new {
        department_id: 1,
        name: "SpongeBob"
    });
    await database.InsertAndCommitAsync("employee", values: new {
        department_id: 1,
        name: "Squidward"
    });
    await database.CommitAsync();
);
```

Creating a DynamicView and echoing data change events on the DynamicView to the console...

```csharp
// Create database instance (will also read the schema from the database)
var database = new SomeDatabase();

// Create a DynamicViewSet that print any data events to the console
// (this will immediately echo an INITIAL data event for each existing matching record)
var dynamicViewSet = database.CreateAndStartDynamicViewSet(
    "SELECT * FROM employee WHERE department_id=@departmentId", 
    new {
        departmentId = 1
    },
    dataEventTransaction => {
        Console.WriteLine(dataEventTransaction);
    }
);

// This will cause the above DynamicViewSet to echo an INSERT data event
await database.InsertAndCommitAsync("employee", values: new {
    department_id: 1
    name: "Mr Crabs"
});

// This will NOT cause the above DynamicViewSet to echo an INSERT data event
// (because the department_id doesn't match)
await database.InsertAndCommitAsync("employee", values: new {
    department_id: 2
    name: "Patrick Star"
});
```

Adding records and echoing all data change events to the console...

```csharp
// Create database instance (will also read the schema from the database)
var database = new SomeDatabase();

// Listen for all database data events
var databaseListener = database.OnNewCommittedTransaction(dataEventTransaction => {
    console.WriteLine($"Low Level DataEventTransaction={dataEventTransaction}");
});

// INSERT a couple of records (this will cause a single data even transaction with
// two INSERT data events to be written to the console above)
using (var transaction = database.BeginTransaction()) {
    await database.InsertAndCommitAsync("employee", values: new {
        department_id: 1,
        name: "SpongeBob"
    });
    await database.InsertAndCommitAsync("employee", values: new {
        department_id: 1,
        name: "Squidward"
    });
    await database.CommitAsync();
);
```

Creating a DynamicView and echoing data change events on the DynamicView to the console...

```csharp
// Create database instance (will also read the schema from the database)
var database = new SomeDatabase();

// Create a DynamicViewSet that print any data events to the console
// (this will immediately echo an INITIAL data event for each existing matching record)
var dynamicViewSet = database.CreateAndStartDynamicViewSet(
    "SELECT * FROM employee WHERE department_id=@departmentId", 
    new {
        departmentId = 1
    },
    dataEventTransaction => {
        Console.WriteLine(dataEventTransaction);
    }
);

// This will cause the above DynamicViewSet to echo an INSERT data event
await database.InsertAndCommitAsync("employee", values: new {
    department_id: 1
    name: "Mr Crabs"
});

// This will NOT cause the above DynamicViewSet to echo an INSERT data event
// (because the department_id doesn't match)
await database.InsertAndCommitAsync("employee", values: new {
    department_id: 2
    name: "Patrick Star"
});
```

Adding records and echoing all data change events to the console...

```csharp
// Create database instance (will also read the schema from the database)
var database = new SomeDatabase();

// Listen for all database data events
var databaseListener = database.OnNewCommittedTransaction(dataEventTransaction => {
    console.WriteLine($"Low Level DataEventTransaction={dataEventTransaction}");
});

// INSERT a couple of records (this will cause a single data even transaction with
// two INSERT data events to be written to the console above)
using (var transaction = database.BeginTransaction()) {
    await database.InsertAndCommitAsync("employee", values: new {
        department_id: 1,
        name: "SpongeBob"
    });
    await database.InsertAndCommitAsync("employee", values: new {
        department_id: 1,
        name: "Squidward"
    });
    await database.CommitAsync();
);
```

Creating a DynamicView and echoing data change events on the DynamicView to the console...

```csharp
// Create database instance (will also read the schema from the database)
var database = new SomeDatabase();

// Create a DynamicViewSet that print any data events to the console
// (this will immediately echo an INITIAL data event for each existing matching record)
var dynamicViewSet = database.CreateAndStartDynamicViewSet(
    "SELECT * FROM employee WHERE department_id=@departmentId", 
    new {
        departmentId = 1
    },
    dataEventTransaction => {
        Console.WriteLine(dataEventTransaction);
    }
);

// This will cause the above DynamicViewSet to echo an INSERT data event
await database.InsertAndCommitAsync("employee", values: new {
    department_id: 1
    name: "Mr Crabs"
});

// This will NOT cause the above DynamicViewSet to echo an INSERT data event
// (because the department_id doesn't match)
await database.InsertAndCommitAsync("employee", values: new {
    department_id: 2
    name: "Patrick Star"
});
```

Adding records and echoing all data change events to the console...

```csharp
// Create database instance (will also read the schema from the database)
var database = new SomeDatabase();

// Listen for all database data events
var databaseListener = database.OnNewCommittedTransaction(dataEventTransaction => {
    console.WriteLine($"Low Level DataEventTransaction={dataEventTransaction}");
});

// INSERT a couple of records (this will cause a single data even transaction with
// two INSERT data events to be written to the console above)
using (var transaction = database.BeginTransaction()) {
    await database.InsertAndCommitAsync("employee", values: new {
        department_id: 1,
        name: "SpongeBob"
    });
    await database.InsertAndCommitAsync("employee", values: new {
        department_id: 1,
        name: "Squidward"
    });
    await database.CommitAsync();
);
```

Creating a DynamicView and echoing data change events on the DynamicView to the console...

```csharp
// Create database instance (will also read the schema from the database)
var database = new SomeDatabase();

// Create a DynamicViewSet that print any data events to the console
// (this will immediately echo an INITIAL data event for each existing matching record)
var dynamicViewSet = database.CreateAndStartDynamicViewSet(
    "SELECT * FROM employee WHERE department_id=@departmentId", 
    new {
        departmentId = 1
    },
    dataEventTransaction => {
        Console.WriteLine(dataEventTransaction);
    }
);

// This will cause the above DynamicViewSet to echo an INSERT data event
await database.InsertAndCommitAsync("employee", values: new {
    department_id: 1
    name: "Mr Crabs"
});

// This will NOT cause the above DynamicViewSet to echo an INSERT data event
// (because the department_id doesn't match)
await database.InsertAndCommitAsync("employee", values: new {
    department_id: 2
    name: "Patrick Star"
});
```

Adding records and echoing all data change events to the console...

```csharp
// Create database instance (will also read the schema from the database)
var database = new SomeDatabase();

// Listen for all database data events
var databaseListener = database.OnNewCommittedTransaction(dataEventTransaction => {
    console.WriteLine($"Low Level DataEventTransaction={dataEventTransaction}");
});

// INSERT a couple of records (this will cause a single data even transaction with
// two INSERT data events to be written to the console above)
using (var transaction = database.BeginTransaction()) {
    await database.InsertAndCommitAsync("employee", values: new {
        department_id: 1,
        name: "SpongeBob"
    });
    await database.InsertAndCommitAsync("employee", values: new {
        department_id: 1,
        name: "Squidward"
    });
    await database.CommitAsync();
);
```

Creating a DynamicView and echoing data change events on the DynamicView to the console...

```csharp
// Create database instance (will also read the schema from the database)
var database = new SomeDatabase();

// Create a DynamicViewSet that print any data events to the console
// (this will immediately echo an INITIAL data event for each existing matching record)
var dynamicViewSet = database.CreateAndStartDynamicViewSet(
    "SELECT * FROM employee WHERE department_id=@departmentId", 
    new {
        departmentId = 1
    },
    dataEventTransaction => {
        Console.WriteLine(dataEventTransaction);
    }
);

// This will cause the above DynamicViewSet to echo an INSERT data event
await database.InsertAndCommitAsync("employee", values: new {
    department_id: 1
    name: "Mr Crabs"
});

// This will NOT cause the above DynamicViewSet to echo an INSERT data event
// (because the department_id doesn't match)
await database.InsertAndCommitAsync("employee", values: new {
    department_id: 2
    name: "Patrick Star"
});
```

Adding records and echoing all data change events to the console...

```csharp
// Create database instance (will also read the schema from the database)
var database = new SomeDatabase();

// Listen for all database data events
var databaseListener = database.OnNewCommittedTransaction(dataEventTransaction => {
    console.WriteLine($"Low Level DataEventTransaction={dataEventTransaction}");
});

// INSERT a couple of records (this will cause a single data even transaction with
// two INSERT data events to be written to the console above)
using (var transaction = database.BeginTransaction()) {
    await database.InsertAndCommitAsync("employee", values: new {
        department_id: 1,
        name: "SpongeBob"
    });
    await database.InsertAndCommitAsync("employee", values: new {
        department_id: 1,
        name: "Squidward"
    });
    await database.CommitAsync();
);
```

Creating a DynamicView and echoing data change events on the DynamicView to the console...

```csharp
// Create database instance (will also read the schema from the database)
var database = new SomeDatabase();

// Create a DynamicViewSet that print any data events to the console
// (this will immediately echo an INITIAL data event for each existing matching record)
var dynamicViewSet = database.CreateAndStartDynamicViewSet(
    "SELECT * FROM employee WHERE department_id=@departmentId", 
    new {
        departmentId = 1
    },
    dataEventTransaction => {
        Console.WriteLine(dataEventTransaction);
    }
);

// This will cause the above DynamicViewSet to echo an INSERT data event
await database.InsertAndCommitAsync("employee", values: new {
    department_id: 1
    name: "Mr Crabs"
});

// This will NOT cause the above DynamicViewSet to echo an INSERT data event
// (because the department_id doesn't match)
await database.InsertAndCommitAsync("employee", values: new {
    department_id: 2
    name: "Patrick Star"
});
```

Adding records and echoing all data change events to the console...

```csharp
// Create database instance (will also read the schema from the database)
var database = new SomeDatabase();

// Listen for all database data events
var databaseListener = database.OnNewCommittedTransaction(dataEventTransaction => {
    console.WriteLine($"Low Level DataEventTransaction={dataEventTransaction}");
});

// INSERT a couple of records (this will cause a single data even transaction with
// two INSERT data events to be written to the console above)
using (var transaction = database.BeginTransaction()) {
    await database.InsertAndCommitAsync("employee", values: new {
        department_id: 1,
        name: "SpongeBob"
    });
    await database.InsertAndCommitAsync("employee", values: new {
        department_id: 1,
        name: "Squidward"
    });
    await database.CommitAsync();
);
```

Creating a DynamicView and echoing data change events on the DynamicView to the console...

```csharp
// Create database instance (will also read the schema from the database)
var database = new SomeDatabase();

// Create a DynamicViewSet that print any data events to the console
// (this will immediately echo an INITIAL data event for each existing matching record)
var dynamicViewSet = database.CreateAndStartDynamicViewSet(
    "SELECT * FROM employee WHERE department_id=@departmentId", 
    new {
        departmentId = 1
    },
    dataEventTransaction => {
        Console.WriteLine(dataEventTransaction);
    }
);

// This will cause the above DynamicViewSet to echo an INSERT data event
await database.InsertAndCommitAsync("employee", values: new {
    department_id: 1
    name: "Mr Crabs"
});

// This will NOT cause the above DynamicViewSet to echo an INSERT data event
// (because the department_id doesn't match)
await database.InsertAndCommitAsync("employee", values: new {
    department_id: 2
    name: "Patrick Star"
});
```

## See Also

* class [BaseDatabase](../Butterfly.Core.Database/BaseDatabase.md)
* namespace [Butterfly.Core.Database.Memory](../Butterfly.Core.md)

<!-- DO NOT EDIT: generated by xmldocmd for Butterfly.Core.dll -->
