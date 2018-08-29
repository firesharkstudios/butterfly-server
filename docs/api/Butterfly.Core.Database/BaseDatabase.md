# BaseDatabase class

Allows executing SELECT statements, creating transactions to execute INSERT, UPDATE, and DELETE statements; creating dynamic views; and receiving data change events both on tables and dynamic views.

Allows executing SELECT statements, creating transactions to execute INSERT, UPDATE, and DELETE statements; creating dynamic views; and receiving data change events both on tables and dynamic views.

```csharp
public abstract class BaseDatabase : IDatabase
```

## Public Members

| name | description |
| --- | --- |
| abstract [CanFieldAlias](BaseDatabase/CanFieldAlias.md) { get; } |  |
| abstract [CanJoin](BaseDatabase/CanJoin.md) { get; } |  |
| [ConnectionString](BaseDatabase/ConnectionString.md) { get; } | Gets or sets the connection string |
| [DeleteCount](BaseDatabase/DeleteCount.md) { get; } |  |
| [InsertCount](BaseDatabase/InsertCount.md) { get; } |  |
| [SelectCount](BaseDatabase/SelectCount.md) { get; } |  |
| [Tables](BaseDatabase/Tables.md) { get; } | Dictionary of [`Table`](Table.md) instances keyed by name |
| [TransactionCount](BaseDatabase/TransactionCount.md) { get; } |  |
| [UpdateCount](BaseDatabase/UpdateCount.md) { get; } |  |
| [AddInputPreprocessor](BaseDatabase/AddInputPreprocessor.md)(…) |  |
| [BeginTransaction](BaseDatabase/BeginTransaction.md)() | Creates a new [`ITransaction`](ITransaction.md) instance. An [`ITransaction`](ITransaction.md) instance allows performing an atomic set of modifications to the database. Must execute [`CommitAsync`](ITransaction/CommitAsync.md) to save the transaction changes. Disposing the transaction without committing rolls back the changes. |
| [BeginTransactionAsync](BaseDatabase/BeginTransactionAsync.md)() | Creates a new [`ITransaction`](ITransaction.md) instance. An [`ITransaction`](ITransaction.md) instance allows performing an atomic set of modifications to the database. Must execute [`CommitAsync`](ITransaction/CommitAsync.md) to save the transaction changes. Disposing the transaction without committing rolls back the changes. |
| [CreateAndStartDynamicViewAsync](BaseDatabase/CreateAndStartDynamicViewAsync.md)(…) | Convenience method which creates a [`DynamicViewSet`](../Butterfly.Core.Database.Dynamic/DynamicViewSet.md), adds a single [`DynamicView`](../Butterfly.Core.Database.Dynamic/DynamicView.md) to the [`DynamicViewSet`](../Butterfly.Core.Database.Dynamic/DynamicViewSet.md), and starts the [`DynamicViewSet`](../Butterfly.Core.Database.Dynamic/DynamicViewSet.md). (2 methods) |
| [CreateDynamicViewSet](BaseDatabase/CreateDynamicViewSet.md)(…) | Allows creating a set of [`DynamicView`](../Butterfly.Core.Database.Dynamic/DynamicView.md) instances that publish a single [`DataEventTransaction`](../Butterfly.Core.Database.Event/DataEventTransaction.md) instance with initial data and new [`DataEventTransaction`](../Butterfly.Core.Database.Event/DataEventTransaction.md) instances when data changes. The [`DataEventTransaction`](../Butterfly.Core.Database.Event/DataEventTransaction.md) instances are published to the lambda passed as the *listener*. (2 methods) |
| [CreateFromResourceFileAsync](BaseDatabase/CreateFromResourceFileAsync.md)(…) | Creates database tables from an embedded resource file by internally calling !:CreateFromText(string) with the contents of the embedded resource file" |
| [CreateFromTextAsync](BaseDatabase/CreateFromTextAsync.md)(…) | Creates database tables from a string containing a semicolon delimited series of CREATE statements in MySQL format (will be converted to native database format as appropriate). |
| [DeleteAndCommitAsync](BaseDatabase/DeleteAndCommitAsync.md)(…) | Executes a DELETE statement as a single transaction |
| [GetInitialDataEventsAsync](BaseDatabase/GetInitialDataEventsAsync.md)(…) |  |
| [InsertAndCommitAsync&lt;T&gt;](BaseDatabase/InsertAndCommitAsync.md)(…) | Executes an INSERT statement as a single transaction |
| [OnNewCommittedTransaction](BaseDatabase/OnNewCommittedTransaction.md)(…) | Adds a listener that is invoked when there is a new committed transaction (2 methods) |
| [OnNewUncommittedTransaction](BaseDatabase/OnNewUncommittedTransaction.md)(…) | Adds a listener that is invoked when there is a new uncommitted transaction (2 methods) |
| [QueryRowAsync](BaseDatabase/QueryRowAsync.md)(…) |  |
| [QueryRowsAsync](BaseDatabase/QueryRowsAsync.md)(…) |  |
| [QueryValueAsync&lt;T&gt;](BaseDatabase/QueryValueAsync.md)(…) |  |
| [SelectRowAsync](BaseDatabase/SelectRowAsync.md)(…) | Executes the SELECT statement and return the first row (the SELECT statement may contain vars like @name specified in *vars*) |
| [SelectRowsAsync](BaseDatabase/SelectRowsAsync.md)(…) | Executes the SELECT statement and return the rows (the SELECT statement may contain vars like @name specified in *vars*) (2 methods) |
| [SelectValueAsync&lt;T&gt;](BaseDatabase/SelectValueAsync.md)(…) | Executes the SELECT statement and return the value of the first column of the first row (the SELECT statement may contain vars like @name specified in *vars*). |
| [SetDefaultValue](BaseDatabase/SetDefaultValue.md)(…) | Allows specifying a lambda that creates a default value for a field when executing an INSERT. If *tableName* is null, the *getValue* lambda will be applied to all tables. |
| [SetOverrideValue](BaseDatabase/SetOverrideValue.md)(…) | Allows specifying a lambda that creates an override value for a field when executing both an INSERT and UPDATE. If *tableName* is null, the *getValue* lambda will be applied to all tables. |
| [UpdateAndCommitAsync](BaseDatabase/UpdateAndCommitAsync.md)(…) | Executes an UPDATE statement as a single transaction |
| static [ConvertMySqlType](BaseDatabase/ConvertMySqlType.md)(…) |  |
| static [CopyFieldValueInputPreprocessor](BaseDatabase/CopyFieldValueInputPreprocessor.md)(…) |  |
| static [RemapTypeInputPreprocessor&lt;T&gt;](BaseDatabase/RemapTypeInputPreprocessor.md)(…) |  |

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

## See Also

* interface [IDatabase](IDatabase.md)
* namespace [Butterfly.Core.Database](../Butterfly.Core.md)

<!-- DO NOT EDIT: generated by xmldocmd for Butterfly.Core.dll -->
