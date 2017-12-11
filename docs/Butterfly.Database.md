# Butterfly.Database assembly

## Butterfly.Database namespace

| public type | description |
| --- | --- |
| abstract class [BaseDatabase](Butterfly.Database/BaseDatabase.md) | Allows executing SELECT statements, creating transactions to execute INSERT, UPDATE, and DELETE statements; creating dynamic views; and receiving data change events both on tables and dynamic views. |
| abstract class [BaseStatement](Butterfly.Database/BaseStatement.md) | Base class for parsing SQL statements |
| abstract class [BaseTransaction](Butterfly.Database/BaseTransaction.md) | Base class implementing [`ITransaction`](Butterfly.Database/ITransaction.md). New implementations will normally extend this class. |
| class [CreateStatement](Butterfly.Database/CreateStatement.md) | Internal class used to parse CREATE statements |
| class [DatabaseException](Butterfly.Database/DatabaseException.md) |  |
| class [DataEventTransactionListener](Butterfly.Database/DataEventTransactionListener.md) | Internal class used to store references to data event transaction listeners |
| class [DeleteStatement](Butterfly.Database/DeleteStatement.md) | Internal class used to parse DELETE statements |
| class [DuplicateKeyDatabaseException](Butterfly.Database/DuplicateKeyDatabaseException.md) |  |
| interface [IDatabase](Butterfly.Database/IDatabase.md) | Allows executing SELECT statements, creating transactions to execute INSERT, UPDATE, and DELETE statements; creating dynamic views; and receiving data change events both on tables and dynamic views. |
| interface [IDynamicParam](Butterfly.Database/IDynamicParam.md) | Use to implement a parameter value that can change |
| class [InsertStatement](Butterfly.Database/InsertStatement.md) | Internal class used to parse INSERT statements |
| interface [ITransaction](Butterfly.Database/ITransaction.md) | Allows executing a series of INSERT, UPDATE, and DELETE actions atomically and publishing a single [`DataEventTransaction`](Butterfly.Database.Event/DataEventTransaction.md) on the underlying [`IDatabase`](Butterfly.Database/IDatabase.md) instance when the transaction is committed. |
| class [SelectStatement](Butterfly.Database/SelectStatement.md) | Internal class used to parse SELECT statements |
| class [StatementEqualsRef](Butterfly.Database/StatementEqualsRef.md) | Internal class representing a SQL equality reference like "table_alias.field_name=@param_name" |
| class [StatementFieldRef](Butterfly.Database/StatementFieldRef.md) | Internal class representing a SQL field reference like "table_alias.field_name field_alias" |
| class [StatementTableRef](Butterfly.Database/StatementTableRef.md) | Internal class representing a SQL table reference like "table_name table_alias" |
| class [Table](Butterfly.Database/Table.md) | Represents a table in an [`IDatabase`](Butterfly.Database/IDatabase.md) |
| class [TableFieldDef](Butterfly.Database/TableFieldDef.md) | Defines a field definition for a [`Table`](Butterfly.Database/Table.md) |
| class [TableIndex](Butterfly.Database/TableIndex.md) | Defines an index for a [`Table`](Butterfly.Database/Table.md) |
| class [UpdateStatement](Butterfly.Database/UpdateStatement.md) | Internal class used to parse UPDATE statements |

## Butterfly.Database.Dynamic namespace

| public type | description |
| --- | --- |
| abstract class [BaseDynamicParam](Butterfly.Database.Dynamic/BaseDynamicParam.md) | Base class for implementing dynamic params (see [`IDynamicParam`](Butterfly.Database/IDynamicParam.md)) |
| class [ChildDynamicParam](Butterfly.Database.Dynamic/ChildDynamicParam.md) |  |
| class [DynamicView](Butterfly.Database.Dynamic/DynamicView.md) | Represents a specific view (SELECT statement) that should be executed to return the initial data as a sequence of [`DataEvent`](Butterfly.Database.Event/DataEvent.md) instances and should publish [`DataEvent`](Butterfly.Database.Event/DataEvent.md) instances when any data in the view changes |
| class [DynamicViewSet](Butterfly.Database.Dynamic/DynamicViewSet.md) | Represents a collection of [`DynamicView`](Butterfly.Database.Dynamic/DynamicView.md) instances. Often a [`DynamicViewSet`](Butterfly.Database.Dynamic/DynamicViewSet.md) instance will represent all the data that should be replicated to a specific client. |
| class [MultiValueDynamicParam](Butterfly.Database.Dynamic/MultiValueDynamicParam.md) | A [`IDynamicParam`](Butterfly.Database/IDynamicParam.md) that may contain multiple values (like an array) |
| class [SingleValueDynamicParam](Butterfly.Database.Dynamic/SingleValueDynamicParam.md) | A [`IDynamicParam`](Butterfly.Database/IDynamicParam.md) that may only contain a single value |

## Butterfly.Database.Event namespace

| public type | description |
| --- | --- |
| class [DataEvent](Butterfly.Database.Event/DataEvent.md) | Represents the initial data or a change in the data. The [`dataEventType`](Butterfly.Database.Event/DataEvent/dataEventType.md) indicates the type of change and the [`name`](Butterfly.Database.Event/DataEvent/name.md) indicates the table or view name. |
| class [DataEventTransaction](Butterfly.Database.Event/DataEventTransaction.md) | Represents a series of [`DataEvent`](Butterfly.Database.Event/DataEvent.md) instances resulting either from an initial query or from committing an [`IDatabase`](Butterfly.Database/IDatabase.md) transaction |
| enum [DataEventType](Butterfly.Database.Event/DataEventType.md) |  |
| class [InitialBeginDataEvent](Butterfly.Database.Event/InitialBeginDataEvent.md) |  |
| class [KeyValueDataEvent](Butterfly.Database.Event/KeyValueDataEvent.md) |  |
| class [RecordDataEvent](Butterfly.Database.Event/RecordDataEvent.md) |  |
| enum [TransactionState](Butterfly.Database.Event/TransactionState.md) |  |

<!-- DO NOT EDIT: generated by xmldocmd for Butterfly.Database.dll -->
