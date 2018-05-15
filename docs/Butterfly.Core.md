# Butterfly.Core assembly

## Butterfly.Core.Auth namespace

| public type | description |
| --- | --- |
| class [AuthManager](Butterfly.Core.Auth/AuthManager.md) | Provides an API to register and login users, handle forgot password and reset password requests, and validate auth tokens. |
| class [AuthToken](Butterfly.Core.Auth/AuthToken.md) | Represents the result of a successful [`LoginAsync`](Butterfly.Core.Auth/AuthManager/LoginAsync.md) or [`RegisterAsync`](Butterfly.Core.Auth/AuthManager/RegisterAsync.md) |

## Butterfly.Core.Channel namespace

| public type | description |
| --- | --- |
| abstract class [BaseChannelServer](Butterfly.Core.Channel/BaseChannelServer.md) | Allows clients to create new channels to the server and allows the server to push messages to connected clients. |
| abstract class [BaseChannelServerConnection](Butterfly.Core.Channel/BaseChannelServerConnection.md) | Internal interface representing a communications channel from the server to the client (might be implemented via WebSockets, HTTP long polling, etc) |
| class [Channel](Butterfly.Core.Channel/Channel.md) |  |
| interface [IChannelServer](Butterfly.Core.Channel/IChannelServer.md) | Allows clients to create new channels to the server and allows the server to push messages to connected clients. |
| interface [IChannelServerConnection](Butterfly.Core.Channel/IChannelServerConnection.md) | Internal interface representing a communications channel from the server to the client (might be implemented via WebSockets, HTTP long polling, etc) |
| class [RegisteredChannel](Butterfly.Core.Channel/RegisteredChannel.md) | Internal class used to store references to new channel listeners |
| class [RegisteredRoute](Butterfly.Core.Channel/RegisteredRoute.md) | Internal class used to store references to new channel listeners |

## Butterfly.Core.Database namespace

| public type | description |
| --- | --- |
| abstract class [BaseDatabase](Butterfly.Core.Database/BaseDatabase.md) | Allows executing SELECT statements, creating transactions to execute INSERT, UPDATE, and DELETE statements; creating dynamic views; and receiving data change events both on tables and dynamic views. |
| abstract class [BaseStatement](Butterfly.Core.Database/BaseStatement.md) | Base class for parsing SQL statements |
| abstract class [BaseTransaction](Butterfly.Core.Database/BaseTransaction.md) | Allows executing a series of INSERT, UPDATE, and DELETE actions atomically and publishing a single !:DataEventTransaction on the underlying [`IDatabase`](Butterfly.Core.Database/IDatabase.md) instance when the transaction is committed. |
| class [CreateStatement](Butterfly.Core.Database/CreateStatement.md) | Internal class used to parse CREATE statements |
| class [DatabaseException](Butterfly.Core.Database/DatabaseException.md) |  |
| class [DataEventTransactionListener](Butterfly.Core.Database/DataEventTransactionListener.md) | Internal class used to store references to data event transaction listeners |
| class [DeleteStatement](Butterfly.Core.Database/DeleteStatement.md) | Internal class used to parse DELETE statements |
| class [DuplicateKeyDatabaseException](Butterfly.Core.Database/DuplicateKeyDatabaseException.md) |  |
| interface [IDatabase](Butterfly.Core.Database/IDatabase.md) | Allows executing SELECT statements, creating transactions to execute INSERT, UPDATE, and DELETE statements; creating dynamic views; and receiving data change events both on tables and dynamic views. |
| interface [IDynamicParam](Butterfly.Core.Database/IDynamicParam.md) | Use to implement a parameter value that can change |
| class [InsertStatement](Butterfly.Core.Database/InsertStatement.md) | Internal class used to parse INSERT statements |
| interface [ITransaction](Butterfly.Core.Database/ITransaction.md) | Allows executing a series of INSERT, UPDATE, and DELETE actions atomically and publishing a single !:DataEventTransaction on the underlying [`IDatabase`](Butterfly.Core.Database/IDatabase.md) instance when the transaction is committed. |
| class [SelectStatement](Butterfly.Core.Database/SelectStatement.md) | Internal class used to parse SELECT statements |
| class [StatementEqualsRef](Butterfly.Core.Database/StatementEqualsRef.md) | Internal class representing a SQL equality reference like "table_alias.field_name=@param_name" |
| class [StatementFieldRef](Butterfly.Core.Database/StatementFieldRef.md) | Internal class representing a SQL field reference like "table_alias.field_name field_alias" |
| class [StatementTableRef](Butterfly.Core.Database/StatementTableRef.md) | Internal class representing a SQL table reference like "table_name table_alias" |
| class [Table](Butterfly.Core.Database/Table.md) | Represents a table in an [`IDatabase`](Butterfly.Core.Database/IDatabase.md) |
| class [TableFieldDef](Butterfly.Core.Database/TableFieldDef.md) | Defines a field definition for a [`Table`](Butterfly.Core.Database/Table.md) |
| class [TableIndex](Butterfly.Core.Database/TableIndex.md) | Defines an index for a [`Table`](Butterfly.Core.Database/Table.md) |
| enum [TableIndexType](Butterfly.Core.Database/TableIndexType.md) |  |
| class [UnableToConnectDatabaseException](Butterfly.Core.Database/UnableToConnectDatabaseException.md) |  |
| class [UpdateStatement](Butterfly.Core.Database/UpdateStatement.md) | Internal class used to parse UPDATE statements |

## Butterfly.Core.Database.Dynamic namespace

| public type | description |
| --- | --- |
| abstract class [BaseDynamicParam](Butterfly.Core.Database.Dynamic/BaseDynamicParam.md) | Base class for implementing dynamic params (see [`IDynamicParam`](Butterfly.Core.Database/IDynamicParam.md)) |
| class [ChildDynamicParam](Butterfly.Core.Database.Dynamic/ChildDynamicParam.md) |  |
| class [DynamicView](Butterfly.Core.Database.Dynamic/DynamicView.md) | Represents a specific view (SELECT statement) that should be executed to return the initial data as a sequence of [`DataEvent`](Butterfly.Core.Database.Event/DataEvent.md) instances and should publish [`DataEvent`](Butterfly.Core.Database.Event/DataEvent.md) instances when any data in the view changes |
| class [DynamicViewSet](Butterfly.Core.Database.Dynamic/DynamicViewSet.md) | Represents a collection of [`DynamicView`](Butterfly.Core.Database.Dynamic/DynamicView.md) instances. Often a [`DynamicViewSet`](Butterfly.Core.Database.Dynamic/DynamicViewSet.md) instance will represent all the data that should be replicated to a specific client. |
| class [MultiValueDynamicParam](Butterfly.Core.Database.Dynamic/MultiValueDynamicParam.md) | A [`IDynamicParam`](Butterfly.Core.Database/IDynamicParam.md) that may contain multiple values (like an array) |
| class [SingleValueDynamicParam](Butterfly.Core.Database.Dynamic/SingleValueDynamicParam.md) | A [`IDynamicParam`](Butterfly.Core.Database/IDynamicParam.md) that may only contain a single value |

## Butterfly.Core.Database.Event namespace

| public type | description |
| --- | --- |
| class [DataEvent](Butterfly.Core.Database.Event/DataEvent.md) | Represents the initial data or a change in the data. The [`dataEventType`](Butterfly.Core.Database.Event/DataEvent/dataEventType.md) indicates the type of change and the !:name indicates the table or view name. |
| class [DataEventTransaction](Butterfly.Core.Database.Event/DataEventTransaction.md) | Represents a series of [`DataEvent`](Butterfly.Core.Database.Event/DataEvent.md) instances resulting either from an initial query or from committing an [`IDatabase`](Butterfly.Core.Database/IDatabase.md) transaction |
| enum [DataEventType](Butterfly.Core.Database.Event/DataEventType.md) |  |
| class [InitialBeginDataEvent](Butterfly.Core.Database.Event/InitialBeginDataEvent.md) |  |
| class [InitialEndDataEvent](Butterfly.Core.Database.Event/InitialEndDataEvent.md) |  |
| class [KeyValueDataEvent](Butterfly.Core.Database.Event/KeyValueDataEvent.md) |  |
| class [RecordDataEvent](Butterfly.Core.Database.Event/RecordDataEvent.md) |  |
| enum [TransactionState](Butterfly.Core.Database.Event/TransactionState.md) |  |

## Butterfly.Core.Database.Memory namespace

| public type | description |
| --- | --- |
| class [MemoryDatabase](Butterfly.Core.Database.Memory/MemoryDatabase.md) | Allows executing SELECT statements, creating transactions to execute INSERT, UPDATE, and DELETE statements; creating dynamic views; and receiving data change events both on tables and dynamic views. |
| class [MemoryTable](Butterfly.Core.Database.Memory/MemoryTable.md) |  |
| class [MemoryTransaction](Butterfly.Core.Database.Memory/MemoryTransaction.md) | Allows executing a series of INSERT, UPDATE, and DELETE actions atomically and publishing a single !:DataEventTransaction on the underlying [`IDatabase`](Butterfly.Core.Database/IDatabase.md) instance when the transaction is committed. |

## Butterfly.Core.Notify namespace

| public type | description |
| --- | --- |
| abstract class [BaseNotifyMessageSender](Butterfly.Core.Notify/BaseNotifyMessageSender.md) |  |
| interface [INotifyMessageSender](Butterfly.Core.Notify/INotifyMessageSender.md) |  |
| class [NotifyManager](Butterfly.Core.Notify/NotifyManager.md) |  |
| class [NotifyMessage](Butterfly.Core.Notify/NotifyMessage.md) |  |
| enum [NotifyMessageType](Butterfly.Core.Notify/NotifyMessageType.md) |  |

## Butterfly.Core.Util namespace

| public type | description |
| --- | --- |
| static class [CleverNameX](Butterfly.Core.Util/CleverNameX.md) |  |
| static class [CommandLineUtil](Butterfly.Core.Util/CommandLineUtil.md) |  |
| static class [CompilerUtil](Butterfly.Core.Util/CompilerUtil.md) |  |
| static class [ConsoleUtil](Butterfly.Core.Util/ConsoleUtil.md) |  |
| static class [DateTimeX](Butterfly.Core.Util/DateTimeX.md) |  |
| class [DictionaryItemDisposable&lt;T,U&gt;](Butterfly.Core.Util/DictionaryItemDisposable-2.md) |  |
| static class [DictionaryX](Butterfly.Core.Util/DictionaryX.md) |  |
| static class [DynamicX](Butterfly.Core.Util/DynamicX.md) |  |
| static class [EnvironmentX](Butterfly.Core.Util/EnvironmentX.md) |  |
| static class [FileX](Butterfly.Core.Util/FileX.md) |  |
| interface [IWebRequest](Butterfly.Core.Util/IWebRequest.md) |  |
| static class [IWebRequestX](Butterfly.Core.Util/IWebRequestX.md) |  |
| static class [JsonUtil](Butterfly.Core.Util/JsonUtil.md) |  |
| class [ListItemDisposable&lt;T&gt;](Butterfly.Core.Util/ListItemDisposable-1.md) |  |
| static class [NameValueCollectionX](Butterfly.Core.Util/NameValueCollectionX.md) |  |
| class [PermissionDeniedException](Butterfly.Core.Util/PermissionDeniedException.md) |  |
| static class [SafeUtil](Butterfly.Core.Util/SafeUtil.md) |  |
| static class [StringX](Butterfly.Core.Util/StringX.md) |  |
| class [UnauthorizedException](Butterfly.Core.Util/UnauthorizedException.md) |  |
| static class [UriX](Butterfly.Core.Util/UriX.md) |  |

## Butterfly.Core.Util.Field namespace

| public type | description |
| --- | --- |
| class [EmailFieldValidator](Butterfly.Core.Util.Field/EmailFieldValidator.md) |  |
| class [GenericFieldValidator](Butterfly.Core.Util.Field/GenericFieldValidator.md) |  |
| interface [IFieldValidator](Butterfly.Core.Util.Field/IFieldValidator.md) |  |
| class [NameFieldValidator](Butterfly.Core.Util.Field/NameFieldValidator.md) |  |
| class [PhoneFieldValidator](Butterfly.Core.Util.Field/PhoneFieldValidator.md) |  |

## Butterfly.Core.Util.Job namespace

| public type | description |
| --- | --- |
| interface [IJob](Butterfly.Core.Util.Job/IJob.md) |  |
| class [JobData](Butterfly.Core.Util.Job/JobData.md) |  |
| class [JobManager](Butterfly.Core.Util.Job/JobManager.md) |  |

## Butterfly.Core.WebApi namespace

| public type | description |
| --- | --- |
| abstract class [BaseHttpRequest](Butterfly.Core.WebApi/BaseHttpRequest.md) |  |
| abstract class [BaseWebApiServer](Butterfly.Core.WebApi/BaseWebApiServer.md) | Allows receiving API requests via HTTP (inspired by Node.js' Express) by wrapping existing C# web servers. |
| interface [IHttpRequest](Butterfly.Core.WebApi/IHttpRequest.md) |  |
| interface [IHttpResponse](Butterfly.Core.WebApi/IHttpResponse.md) |  |
| interface [IWebApiServer](Butterfly.Core.WebApi/IWebApiServer.md) | Allows receiving API requests via HTTP (inspired by Node.js' Express) by wrapping existing C# web servers. |
| class [WebHandler](Butterfly.Core.WebApi/WebHandler.md) | Internal class used to store references to new web handlers |

<!-- DO NOT EDIT: generated by xmldocmd for Butterfly.Core.dll -->
