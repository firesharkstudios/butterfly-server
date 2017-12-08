/*
 * Copyright 2017 Fireshark Studios, LLC
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using NLog;

using Butterfly.Database.Dynamic;
using Butterfly.Database.Event;
using Butterfly.Util;

using Dict = System.Collections.Generic.Dictionary<string, object>;

namespace Butterfly.Database {
    /// <inheritdoc/>
    /// <summary>
    /// Base class implementing <see cref="IDatabase"/>. New implementations will normally extend this class.
    /// </summary>
    public abstract class BaseDatabase : IDatabase {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        protected readonly Dictionary<string, Table> tableByName = new Dictionary<string, Table>();

        protected static readonly Regex SQL_COMMENT = new Regex(@"^\-\-(.*)$", RegexOptions.Multiline);

        protected BaseDatabase(string connectionString) {
            this.ConnectionString = connectionString;
            this.LoadSchemaAsync().Wait();
        }

        public string ConnectionString {
            get;
            protected set;
        }

        public Dictionary<string, Table> Tables => this.tableByName;

        public async Task CreateFromResourceFileAsync(Assembly assembly, string resourceFile) {
            //logger.Debug($"CreateFromResourceFileAsync():resourceNames={string.Join(",", assembly.GetManifestResourceNames())}");
            string sql = await FileX.LoadResourceAsTextAsync(assembly, resourceFile);
            await this.CreateFromTextAsync(sql);
        }

        public async Task CreateFromTextAsync(string sql) {
            //logger.Debug($"CreateFromTextAsync():sql={sql}");
            var noCommentSql = SQL_COMMENT.Replace(sql, "");
            var sqlParts = noCommentSql.Split(';').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x));

            List<string> tableSchemasToLoad = new List<string>();
            using (var transaction = await this.BeginTransaction()) {
                foreach (var sqlPart in sqlParts) {
                    if (!string.IsNullOrWhiteSpace(sqlPart)) {
                        CreateStatement statement = this.CreateStatement(sqlPart);
                        if (!this.Tables.Keys.Contains(statement.TableName)) {
                            bool tableSchemaLoaded = await transaction.CreateAsync(statement);
                            if (!tableSchemaLoaded) {
                                tableSchemasToLoad.Add(statement.TableName);
                            }
                        }
                    }
                }
                await transaction.CommitAsync();
            }

            foreach (var tableName in tableSchemasToLoad) {
                Table table = await this.LoadTableSchemaAsync(tableName);
                this.tableByName[table.Name] = table;
            }
        }

        protected virtual CreateStatement CreateStatement(string sql) {
            return new CreateStatement(sql);
        }

        /// <summary>
        /// Loads the database schema from an existing database.
        /// </summary>
        /// <returns></returns>
        protected abstract Task LoadSchemaAsync();

        protected abstract Task<Table> LoadTableSchemaAsync(string tableName);

        // Manage data event transaction listeners
        protected readonly List<DataEventTransactionListener> uncommittedTransactionListeners = new List<DataEventTransactionListener>();
        public IDisposable OnNewUncommittedTransaction(Action<DataEventTransaction> listener) => new ListItemDisposable<DataEventTransactionListener>(uncommittedTransactionListeners, new DataEventTransactionListener(listener));
        public IDisposable OnNewUncommittedTransaction(Func<DataEventTransaction, Task> listener) => new ListItemDisposable<DataEventTransactionListener>(uncommittedTransactionListeners, new DataEventTransactionListener(listener));

        protected readonly List<DataEventTransactionListener> committedTransactionListeners = new List<DataEventTransactionListener>();
        public IDisposable OnNewCommittedTransaction(Action<DataEventTransaction> listener) => new ListItemDisposable<DataEventTransactionListener>(committedTransactionListeners, new DataEventTransactionListener(listener));
        public IDisposable OnNewCommittedTransaction(Func<DataEventTransaction, Task> listener) => new ListItemDisposable<DataEventTransactionListener>(committedTransactionListeners, new DataEventTransactionListener(listener));

        internal async Task ProcessDataEventTransaction(TransactionState transactionState, DataEventTransaction dataEventTransaction) {
            List<Task> tasks = new List<Task>();
            switch (transactionState) {
                case TransactionState.Uncommitted:
                    // Use ToArray() to avoid the collection being modified during the loop
                    foreach (var listener in this.uncommittedTransactionListeners.ToArray()) {
                        if (listener.listener != null) listener.listener(dataEventTransaction);
                        else await listener.listenerAsync(dataEventTransaction);
                    }
                    await Task.WhenAll(tasks);
                    break;
                case TransactionState.Committed:
                    // Use ToArray() to avoid the collection being modified during the loop
                    foreach (var listener in this.committedTransactionListeners.ToArray()) {
                        if (listener.listener != null) listener.listener(dataEventTransaction);
                        else await listener.listenerAsync(dataEventTransaction);
                    }
                    await Task.WhenAll(tasks);
                    break;
            }
        }

        public async Task<DataEventTransaction> GetInitialDataEventTransactionAsync(string statementSql, dynamic statementParams = null) {
            SelectStatement statement = new SelectStatement(this, statementSql);
            DataEvent[] initialDataEvents = await this.GetInitialDataEventsAsync(statement.TableRefs[0].table.Name, statement.TableRefs[0].table.PrimaryIndex.FieldNames, statement, statementParams);
            return new DataEventTransaction(DateTime.Now, initialDataEvents);
        }

        public async Task<DataEvent[]> GetInitialDataEventsAsync(string dataEventName, string[] keyFieldNames, SelectStatement selectStatement, dynamic statementParams = null) {
            logger.Debug($"GetInitialDataEvents():sql={selectStatement.Sql}");

            List<DataEvent> dataEvents = new List<DataEvent>();
            dataEvents.Add(new InitialBeginDataEvent(dataEventName, keyFieldNames));

            Dict[] rows = await this.SelectRowsAsync(selectStatement, statementParams);
            RecordDataEvent[] changeDataEvents = rows.Select(x => new RecordDataEvent(DataEventType.Initial, dataEventName, GetKeyValue(keyFieldNames, x), x)).ToArray();
            dataEvents.AddRange(changeDataEvents);

            dataEvents.Add(new DataEvent(DataEventType.InitialEnd, dataEventName));

            return dataEvents.ToArray();
        }

        public async Task<T> SelectValue<T>(string sql, dynamic values, T defaultValue) {
            Dict row = await this.SelectRowAsync(sql, values);
            if (row == null || !row.TryGetValue(row.Keys.First(), out object value)) return defaultValue;

            return (T)Convert.ChangeType(value, typeof(T));
        }

        public async Task<Dict> SelectRowAsync(string statementSql, dynamic statementParams = null) {
            Dict[] rows = await this.SelectRowsAsync(statementSql, statementParams);
            if (rows.Length == 0) return null;
            else if (rows.Length > 1) throw new Exception("SelectRow returned more than one row");
            return rows.First();
        }

        public async Task<Dict[]> SelectRowsAsync(string statementSql, dynamic statementParams = null) {
            SelectStatement statement = new SelectStatement(this, statementSql);
            return await this.SelectRowsAsync(statement, statementParams);
        }

        public async Task<Dict[]> SelectRowsAsync(SelectStatement statement, dynamic statementParams, string newAndCondition, Dict newWhereParams) {
            string overrideWhereClause = string.IsNullOrEmpty(statement.whereClause) ? newAndCondition : $"({newAndCondition}) AND ({statement.whereClause})";
            SelectStatement newStatement = new SelectStatement(statement, overrideWhereClause, true);
            Dict newStatementParamsDict = newStatement.ConvertParamsToDict(statementParams);
            newStatementParamsDict.UpdateFrom(newWhereParams);
            (string executableSql, Dict executableParams) = newStatement.GetExecutableSqlAndParams(newStatementParamsDict);
            return await this.DoSelectRowsAsync(executableSql, executableParams);
        }

        public async Task<Dict[]> SelectRowsAsync(SelectStatement statement, dynamic statementParams) {
            Dict statementParamsDict = statement.ConvertParamsToDict(statementParams);
            BaseStatement.ConfirmAllParamsUsed(statement.Sql, statementParamsDict);
            (string executableSql, Dict executableParams) = statement.GetExecutableSqlAndParams(statementParamsDict);
            return await this.DoSelectRowsAsync(executableSql, executableParams);
        }

        protected abstract Task<Dict[]> DoSelectRowsAsync(string executableSql, Dict executableParams);

        public async Task<object> InsertAndCommitAsync(string sql, dynamic record, bool ignoreIfDuplicate = false) {
            object result;
            using (var transaction = await this.BeginTransaction()) {
                result = await transaction.InsertAsync(sql, record, ignoreIfDuplicate);
                await transaction.CommitAsync();
            }
            return result;
        }

        public async Task<int> UpdateAndCommitAsync(string sourceSql, dynamic sourceParams) {
            int count;
            using (var transaction = await this.BeginTransaction()) {
                count = await transaction.UpdateAsync(sourceSql, sourceParams);
                await transaction.CommitAsync();
            }
            return count;
        }

        public async Task<int> DeleteAndCommitAsync(string sql, dynamic whereParams) {
            int count;
            using (var transaction = await this.BeginTransaction()) {
                count = await transaction.DeleteAsync(sql, whereParams);
                await transaction.CommitAsync();
            }
            return count;
        }

        public async Task<ITransaction> BeginTransaction() {
            ITransaction transaction = this.CreateTransaction();
            await transaction.BeginAsync();
            return transaction;
        }

        protected abstract ITransaction CreateTransaction();

        protected readonly Dictionary<string, Func<object>> getDefaultValueByFieldName = new Dictionary<string, Func<object>>();
        public void SetInsertDefaultValue(string fieldName, Func<object> getDefaultValue, string tableName = null) {
            if (tableName == null) {
                this.getDefaultValueByFieldName[fieldName] = getDefaultValue;
            }
            else {
                if (!this.Tables.TryGetValue(tableName, out Table table)) throw new Exception($"Invalid table name '{tableName}'");
                table.SetDefaultValue(fieldName, getDefaultValue);
            }
        }

        public Dict GetInsertDefaultValues(Table table) {
            Dictionary<string, object> defaultValues = new Dict();
            foreach ((string fieldName, Func<object> getDefaultValue) in table.GetDefaultValueByFieldName) {
                TableFieldDef fieldDef = table.FindFieldDef(fieldName);
                if (fieldDef!=null && !defaultValues.ContainsKey(fieldDef.name)) defaultValues[fieldDef.name] = getDefaultValue();
            }
            foreach ((string fieldName, Func<object> getDefaultValue) in this.getDefaultValueByFieldName) {
                TableFieldDef fieldDef = table.FindFieldDef(fieldName);
                if (fieldDef != null && !defaultValues.ContainsKey(fieldDef.name)) defaultValues[fieldDef.name] = getDefaultValue();
            }
            return defaultValues;
        }

        protected readonly static Regex PARSE_TYPE = new Regex(@"^(?<type>.+?)(?<maxLengthWithParens>\(\d+\))?$");

        public static (Type, int) ConvertMySqlType(string text) {
            Match match = PARSE_TYPE.Match(text);
            if (!match.Success) throw new Exception($"Could not parse SQL type '{text}'");

            string typeText = match.Groups["type"].Value;

            Type type;
            if (typeText.Equals("VARCHAR", StringComparison.OrdinalIgnoreCase) || typeText.Equals("CHAR", StringComparison.OrdinalIgnoreCase)) {
                type = typeof(string);
            }
            else if (typeText.Equals("TINYINT", StringComparison.OrdinalIgnoreCase)) {
                type = typeof(byte);
            }
            else if (typeText.Equals("MEDIUMINT", StringComparison.OrdinalIgnoreCase)) {
                type = typeof(int);
            }
            else if (typeText.Equals("INT", StringComparison.OrdinalIgnoreCase)) {
                type = typeof(long);
            }
            else if (typeText.Equals("BIGINT", StringComparison.OrdinalIgnoreCase)) {
                type = typeof(long);
            }
            else if (typeText.Equals("FLOAT", StringComparison.OrdinalIgnoreCase)) {
                type = typeof(float);
            }
            else if (typeText.Equals("DOUBLE", StringComparison.OrdinalIgnoreCase)) {
                type = typeof(double);
            }
            else if (typeText.Equals("DATETIME", StringComparison.OrdinalIgnoreCase)) {
                type = typeof(DateTime);
            }
            else {
                throw new Exception($"Unknown field type '{text}'");
            }

            string maxLengthText = match.Groups["maxLengthWithParens"].Value.Replace("(", "").Replace(")", "");
            if (!int.TryParse(maxLengthText, out int maxLength)) maxLength = -1;

            return (type, maxLength);
        }

        public DynamicViewSet CreateDynamicViewSet(Action<DataEventTransaction> listener) {
            return new DynamicViewSet(this, listener);
        }

        public DynamicViewSet CreateDynamicViewSet(Func<DataEventTransaction, Task> asyncListener) {
            return new DynamicViewSet(this, asyncListener);
        }

        public async Task<DynamicViewSet> CreateAndStartDynamicView(string sql, Action<DataEventTransaction> listener, dynamic values = null, string name = null, string[] keyFieldNames = null) {
            var dynamicViewSet = this.CreateDynamicViewSet(listener);
            dynamicViewSet.CreateDynamicView(sql, values, name, keyFieldNames);
            return await dynamicViewSet.StartAsync();
        }

        public async Task<DynamicViewSet> CreateAndStartDynamicView(string sql, Func<DataEventTransaction, Task> asyncListener, dynamic values = null, string name = null, string[] keyFieldNames = null) {
            var dynamicViewSet = this.CreateDynamicViewSet(asyncListener);
            dynamicViewSet.CreateDynamicView(sql, values, name, keyFieldNames);
            return await dynamicViewSet.StartAsync();
        }

        public static object GetKeyValue(string[] fieldNames, Dict record) {
            StringBuilder sb = new StringBuilder();
            bool isFirst = true;
            foreach (var fieldName in fieldNames) {
                if (isFirst) isFirst = false;
                else sb.Append(";");

                if (!record.ContainsKey(fieldName)) throw new Exception($"Could not get key field '{fieldName}' to build key value");
                sb.Append(record[fieldName]);
            }
            return sb.ToString();
        }

        public static Dict ParseKeyValue(object keyValue, string[] keyFieldNames) {
            Dict result = new Dict();
            if (keyValue is string keyValueText) {
                string[] keyValueParts = keyValueText.Split(';');
                for (int i = 0; i < keyFieldNames.Length; i++) {
                    result[keyFieldNames[i]] = keyValueParts[i];
                }
            }
            else if (keyFieldNames.Length==1) {
                result[keyFieldNames[0]] = keyValue;
            }
            else {
                throw new Exception("Cannot parse key value that is not a string and keyFieldNames.Length!=1");
            }
            return result;
        }
    }

    public class DatabaseException : Exception {
        public DatabaseException(string message) : base(message) {
        }
    }

    public class DuplicateKeyDatabaseException : DatabaseException {
        public DuplicateKeyDatabaseException(string message) : base(message) {
        }
    }
}
