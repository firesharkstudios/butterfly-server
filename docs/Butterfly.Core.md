# Butterfly.Core assembly

## Butterfly.Core.Auth namespace

| public type | description |
| --- | --- |
| class [AuthManager](Butterfly.Core.Auth/AuthManager.md) |  |
| class [AuthToken](Butterfly.Core.Auth/AuthToken.md) |  |

## Butterfly.Core.Channel namespace

| public type | description |
| --- | --- |
| abstract class [BaseChannelServer](Butterfly.Core.Channel/BaseChannelServer.md) |  |
| abstract class [BaseChannelServerConnection](Butterfly.Core.Channel/BaseChannelServerConnection.md) |  |
| class [Channel](Butterfly.Core.Channel/Channel.md) |  |
| class [ChannelSubscription](Butterfly.Core.Channel/ChannelSubscription.md) |  |
| interface [IChannelServer](Butterfly.Core.Channel/IChannelServer.md) |  |
| interface [IChannelServerConnection](Butterfly.Core.Channel/IChannelServerConnection.md) |  |

## Butterfly.Core.Database namespace

| public type | description |
| --- | --- |
| abstract class [BaseDatabase](Butterfly.Core.Database/BaseDatabase.md) |  |
| abstract class [BaseStatement](Butterfly.Core.Database/BaseStatement.md) |  |
| abstract class [BaseTransaction](Butterfly.Core.Database/BaseTransaction.md) |  |
| class [CreateStatement](Butterfly.Core.Database/CreateStatement.md) |  |
| class [DatabaseException](Butterfly.Core.Database/DatabaseException.md) |  |
| class [DataEventTransactionListener](Butterfly.Core.Database/DataEventTransactionListener.md) |  |
| class [DeleteStatement](Butterfly.Core.Database/DeleteStatement.md) |  |
| class [DuplicateKeyDatabaseException](Butterfly.Core.Database/DuplicateKeyDatabaseException.md) |  |
| interface [IDatabase](Butterfly.Core.Database/IDatabase.md) |  |
| interface [IDynamicParam](Butterfly.Core.Database/IDynamicParam.md) |  |
| class [InsertStatement](Butterfly.Core.Database/InsertStatement.md) |  |
| interface [ITransaction](Butterfly.Core.Database/ITransaction.md) |  |
| enum [JoinType](Butterfly.Core.Database/JoinType.md) |  |
| class [SelectStatement](Butterfly.Core.Database/SelectStatement.md) |  |
| class [StatementEqualsRef](Butterfly.Core.Database/StatementEqualsRef.md) |  |
| class [StatementFieldRef](Butterfly.Core.Database/StatementFieldRef.md) |  |
| class [StatementFromRef](Butterfly.Core.Database/StatementFromRef.md) |  |
| class [Table](Butterfly.Core.Database/Table.md) |  |
| class [TableFieldDef](Butterfly.Core.Database/TableFieldDef.md) |  |
| class [TableIndex](Butterfly.Core.Database/TableIndex.md) |  |
| enum [TableIndexType](Butterfly.Core.Database/TableIndexType.md) |  |
| class [UnableToConnectDatabaseException](Butterfly.Core.Database/UnableToConnectDatabaseException.md) |  |
| class [UpdateStatement](Butterfly.Core.Database/UpdateStatement.md) |  |

## Butterfly.Core.Database.Dynamic namespace

| public type | description |
| --- | --- |
| abstract class [BaseDynamicParam](Butterfly.Core.Database.Dynamic/BaseDynamicParam.md) |  |
| class [ChildDynamicParam](Butterfly.Core.Database.Dynamic/ChildDynamicParam.md) |  |
| class [DynamicView](Butterfly.Core.Database.Dynamic/DynamicView.md) |  |
| class [DynamicViewSet](Butterfly.Core.Database.Dynamic/DynamicViewSet.md) |  |
| class [MultiValueDynamicParam](Butterfly.Core.Database.Dynamic/MultiValueDynamicParam.md) |  |
| class [SingleValueDynamicParam](Butterfly.Core.Database.Dynamic/SingleValueDynamicParam.md) |  |

## Butterfly.Core.Database.Event namespace

| public type | description |
| --- | --- |
| class [DataEvent](Butterfly.Core.Database.Event/DataEvent.md) |  |
| class [DataEventTransaction](Butterfly.Core.Database.Event/DataEventTransaction.md) |  |
| enum [DataEventType](Butterfly.Core.Database.Event/DataEventType.md) |  |
| class [InitialBeginDataEvent](Butterfly.Core.Database.Event/InitialBeginDataEvent.md) |  |
| class [InitialEndDataEvent](Butterfly.Core.Database.Event/InitialEndDataEvent.md) |  |
| class [KeyValueDataEvent](Butterfly.Core.Database.Event/KeyValueDataEvent.md) |  |
| class [RecordDataEvent](Butterfly.Core.Database.Event/RecordDataEvent.md) |  |
| enum [TransactionState](Butterfly.Core.Database.Event/TransactionState.md) |  |

## Butterfly.Core.Database.Memory namespace

| public type | description |
| --- | --- |
| class [MemoryDatabase](Butterfly.Core.Database.Memory/MemoryDatabase.md) |  |
| class [MemoryTable](Butterfly.Core.Database.Memory/MemoryTable.md) |  |
| class [MemoryTransaction](Butterfly.Core.Database.Memory/MemoryTransaction.md) |  |

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
| static class [ProcessX](Butterfly.Core.Util/ProcessX.md) |  |
| static class [RandomUtil](Butterfly.Core.Util/RandomUtil.md) |  |
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
| abstract class [BaseWebApiServer](Butterfly.Core.WebApi/BaseWebApiServer.md) |  |
| interface [IHttpRequest](Butterfly.Core.WebApi/IHttpRequest.md) |  |
| interface [IHttpResponse](Butterfly.Core.WebApi/IHttpResponse.md) |  |
| interface [IWebApiServer](Butterfly.Core.WebApi/IWebApiServer.md) |  |
| class [WebHandler](Butterfly.Core.WebApi/WebHandler.md) |  |

<!-- DO NOT EDIT: generated by xmldocmd for Butterfly.Core.dll -->
