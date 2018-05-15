# Butterfly.Core assembly

## Butterfly.Auth namespace

| public type | description |
| --- | --- |
| class [AuthManager](Butterfly.Auth/AuthManager.md) | Provides an API to register and login users, handle forgot password and reset password requests, and validate auth tokens. |
| class [AuthToken](Butterfly.Auth/AuthToken.md) | Represents the result of a successful [`LoginAsync`](Butterfly.Auth/AuthManager/LoginAsync.md) or [`RegisterAsync`](Butterfly.Auth/AuthManager/RegisterAsync.md) |

## Butterfly.Channel namespace

| public type | description |
| --- | --- |
| abstract class [BaseChannelServer](Butterfly.Channel/BaseChannelServer.md) | Allows clients to create new channels to the server and allows the server to push messages to connected clients. |
| abstract class [BaseChannelServerConnection](Butterfly.Channel/BaseChannelServerConnection.md) | Internal interface representing a communications channel from the server to the client (might be implemented via WebSockets, HTTP long polling, etc) |
| class [Channel](Butterfly.Channel/Channel.md) |  |
| interface [IChannelServer](Butterfly.Channel/IChannelServer.md) | Allows clients to create new channels to the server and allows the server to push messages to connected clients. |
| interface [IChannelServerConnection](Butterfly.Channel/IChannelServerConnection.md) | Internal interface representing a communications channel from the server to the client (might be implemented via WebSockets, HTTP long polling, etc) |
| class [RegisteredChannel](Butterfly.Channel/RegisteredChannel.md) | Internal class used to store references to new channel listeners |
| class [RegisteredRoute](Butterfly.Channel/RegisteredRoute.md) | Internal class used to store references to new channel listeners |

## Butterfly.Database namespace

| public type | description |
| --- | --- |
| abstract class [BaseDatabase](Butterfly.Database/BaseDatabase.md) | Allows executing SELECT statements, creating transactions to execute INSERT, UPDATE, and DELETE statements; creating dynamic views; and receiving data change events both on tables and dynamic views. |
| abstract class [BaseStatement](Butterfly.Database/BaseStatement.md) | Base class for parsing SQL statements |
| abstract class [BaseTransaction](Butterfly.Database/BaseTransaction.md) | Allows executing a series of INSERT, UPDATE, and DELETE actions atomically and publishing a single !:DataEventTransaction on the underlying [`IDatabase`](Butterfly.Database/IDatabase.md) instance when the transaction is committed. |
| class [CreateStatement](Butterfly.Database/CreateStatement.md) | Internal class used to parse CREATE statements |
| class [DatabaseException](Butterfly.Database/DatabaseException.md) |  |
| class [DataEventTransactionListener](Butterfly.Database/DataEventTransactionListener.md) | Internal class used to store references to data event transaction listeners |
| class [DeleteStatement](Butterfly.Database/DeleteStatement.md) | Internal class used to parse DELETE statements |
| class [DuplicateKeyDatabaseException](Butterfly.Database/DuplicateKeyDatabaseException.md) |  |
| interface [IDatabase](Butterfly.Database/IDatabase.md) | Allows executing SELECT statements, creating transactions to execute INSERT, UPDATE, and DELETE statements; creating dynamic views; and receiving data change events both on tables and dynamic views. |
| interface [IDynamicParam](Butterfly.Database/IDynamicParam.md) | Use to implement a parameter value that can change |
| class [InsertStatement](Butterfly.Database/InsertStatement.md) | Internal class used to parse INSERT statements |
| interface [ITransaction](Butterfly.Database/ITransaction.md) | Allows executing a series of INSERT, UPDATE, and DELETE actions atomically and publishing a single !:DataEventTransaction on the underlying [`IDatabase`](Butterfly.Database/IDatabase.md) instance when the transaction is committed. |
| class [SelectStatement](Butterfly.Database/SelectStatement.md) | Internal class used to parse SELECT statements |
| class [StatementEqualsRef](Butterfly.Database/StatementEqualsRef.md) | Internal class representing a SQL equality reference like "table_alias.field_name=@param_name" |
| class [StatementFieldRef](Butterfly.Database/StatementFieldRef.md) | Internal class representing a SQL field reference like "table_alias.field_name field_alias" |
| class [StatementTableRef](Butterfly.Database/StatementTableRef.md) | Internal class representing a SQL table reference like "table_name table_alias" |
| class [Table](Butterfly.Database/Table.md) | Represents a table in an [`IDatabase`](Butterfly.Database/IDatabase.md) |
| class [TableFieldDef](Butterfly.Database/TableFieldDef.md) | Defines a field definition for a [`Table`](Butterfly.Database/Table.md) |
| class [TableIndex](Butterfly.Database/TableIndex.md) | Defines an index for a [`Table`](Butterfly.Database/Table.md) |
| enum [TableIndexType](Butterfly.Database/TableIndexType.md) |  |
| class [UnableToConnectDatabaseException](Butterfly.Database/UnableToConnectDatabaseException.md) |  |
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
| class [DataEvent](Butterfly.Database.Event/DataEvent.md) | Represents the initial data or a change in the data. The [`dataEventType`](Butterfly.Database.Event/DataEvent/dataEventType.md) indicates the type of change and the !:name indicates the table or view name. |
| class [DataEventTransaction](Butterfly.Database.Event/DataEventTransaction.md) | Represents a series of [`DataEvent`](Butterfly.Database.Event/DataEvent.md) instances resulting either from an initial query or from committing an [`IDatabase`](Butterfly.Database/IDatabase.md) transaction |
| enum [DataEventType](Butterfly.Database.Event/DataEventType.md) |  |
| class [InitialBeginDataEvent](Butterfly.Database.Event/InitialBeginDataEvent.md) |  |
| class [InitialEndDataEvent](Butterfly.Database.Event/InitialEndDataEvent.md) |  |
| class [KeyValueDataEvent](Butterfly.Database.Event/KeyValueDataEvent.md) |  |
| class [RecordDataEvent](Butterfly.Database.Event/RecordDataEvent.md) |  |
| enum [TransactionState](Butterfly.Database.Event/TransactionState.md) |  |

## Butterfly.Database.Memory namespace

| public type | description |
| --- | --- |
| class [MemoryDatabase](Butterfly.Database.Memory/MemoryDatabase.md) | Allows executing SELECT statements, creating transactions to execute INSERT, UPDATE, and DELETE statements; creating dynamic views; and receiving data change events both on tables and dynamic views. |
| class [MemoryTable](Butterfly.Database.Memory/MemoryTable.md) |  |
| class [MemoryTransaction](Butterfly.Database.Memory/MemoryTransaction.md) | Allows executing a series of INSERT, UPDATE, and DELETE actions atomically and publishing a single !:DataEventTransaction on the underlying [`IDatabase`](Butterfly.Database/IDatabase.md) instance when the transaction is committed. |

## Butterfly.Notify namespace

| public type | description |
| --- | --- |
| abstract class [BaseNotifyMessageSender](Butterfly.Notify/BaseNotifyMessageSender.md) |  |
| interface [INotifyMessageSender](Butterfly.Notify/INotifyMessageSender.md) |  |
| class [NotifyManager](Butterfly.Notify/NotifyManager.md) |  |
| class [NotifyMessage](Butterfly.Notify/NotifyMessage.md) |  |
| enum [NotifyMessageType](Butterfly.Notify/NotifyMessageType.md) |  |

## Butterfly.Util namespace

| public type | description |
| --- | --- |
| static class [CleverNameX](Butterfly.Util/CleverNameX.md) |  |
| static class [CommandLineUtil](Butterfly.Util/CommandLineUtil.md) |  |
| static class [CompilerUtil](Butterfly.Util/CompilerUtil.md) |  |
| static class [ConsoleUtil](Butterfly.Util/ConsoleUtil.md) |  |
| static class [DateTimeX](Butterfly.Util/DateTimeX.md) |  |
| class [DictionaryItemDisposable&lt;T,U&gt;](Butterfly.Util/DictionaryItemDisposable-2.md) |  |
| static class [DictionaryX](Butterfly.Util/DictionaryX.md) |  |
| static class [DynamicX](Butterfly.Util/DynamicX.md) |  |
| static class [EnvironmentX](Butterfly.Util/EnvironmentX.md) |  |
| static class [FileX](Butterfly.Util/FileX.md) |  |
| interface [IWebRequest](Butterfly.Util/IWebRequest.md) |  |
| static class [IWebRequestX](Butterfly.Util/IWebRequestX.md) |  |
| static class [JsonUtil](Butterfly.Util/JsonUtil.md) |  |
| class [ListItemDisposable&lt;T&gt;](Butterfly.Util/ListItemDisposable-1.md) |  |
| static class [NameValueCollectionX](Butterfly.Util/NameValueCollectionX.md) |  |
| class [PermissionDeniedException](Butterfly.Util/PermissionDeniedException.md) |  |
| static class [SafeUtil](Butterfly.Util/SafeUtil.md) |  |
| static class [StringX](Butterfly.Util/StringX.md) |  |
| class [UnauthorizedException](Butterfly.Util/UnauthorizedException.md) |  |
| static class [UriX](Butterfly.Util/UriX.md) |  |

## Butterfly.Util.Field namespace

| public type | description |
| --- | --- |
| class [EmailFieldValidator](Butterfly.Util.Field/EmailFieldValidator.md) |  |
| class [GenericFieldValidator](Butterfly.Util.Field/GenericFieldValidator.md) |  |
| interface [IFieldValidator](Butterfly.Util.Field/IFieldValidator.md) |  |
| class [NameFieldValidator](Butterfly.Util.Field/NameFieldValidator.md) |  |
| class [PhoneFieldValidator](Butterfly.Util.Field/PhoneFieldValidator.md) |  |

## Butterfly.Util.Job namespace

| public type | description |
| --- | --- |
| interface [IJob](Butterfly.Util.Job/IJob.md) |  |
| class [JobData](Butterfly.Util.Job/JobData.md) |  |
| class [JobManager](Butterfly.Util.Job/JobManager.md) |  |

## Butterfly.WebApi namespace

| public type | description |
| --- | --- |
| abstract class [BaseHttpRequest](Butterfly.WebApi/BaseHttpRequest.md) |  |
| abstract class [BaseWebApiServer](Butterfly.WebApi/BaseWebApiServer.md) | Allows receiving API requests via HTTP (inspired by Node.js' Express) by wrapping existing C# web servers. |
| interface [IHttpRequest](Butterfly.WebApi/IHttpRequest.md) |  |
| interface [IHttpResponse](Butterfly.WebApi/IHttpResponse.md) |  |
| interface [IWebApiServer](Butterfly.WebApi/IWebApiServer.md) | Allows receiving API requests via HTTP (inspired by Node.js' Express) by wrapping existing C# web servers. |
| class [WebHandler](Butterfly.WebApi/WebHandler.md) | Internal class used to store references to new web handlers |

<!-- DO NOT EDIT: generated by xmldocmd for Butterfly.Core.dll -->
