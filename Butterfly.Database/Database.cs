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
using System.IO;
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

    public abstract class Database {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        protected readonly Dictionary<string, Table> tableByName = new Dictionary<string, Table>();

        public Dictionary<string, Table> Tables => this.tableByName;

        protected readonly static Regex SQL_COMMENT = new Regex(@"^\-\-(.*)$", RegexOptions.Multiline);

        public Database(string connectionString) {
            this.ConnectionString = connectionString;
            this.LoadSchemaAsync().Wait();
        }

        public string ConnectionString {
            get;
            protected set;
        }

        /// <summary>
        /// Creates database tables from an embedded resource file by internally calling CreateFromTextAsync with the contents of the embedded resource file (<see cref="CreateFromTextAsync(string)"/>.
        /// </summary>
        /// <param name="resourceFile"></param>
        /// <returns></returns>
        public async Task CreateFromResourceFileAsync(Assembly assembly, string resourceFile) {
            //logger.Debug($"CreateFromResourceFileAsync():resourceNames={string.Join(",", assembly.GetManifestResourceNames())}");
            string sql = await FileX.LoadResourceAsTextAsync(assembly, resourceFile);
            await this.CreateFromTextAsync(sql);
        }

        /// <summary>
        /// Creates database tables from a string containing a semicolon delimited series of CREATE statements in MySQL format (will be converted to native database format as appropriate).<para/>
        /// Comments (lines beginning with --) will be ignored.<para/>
        /// Each CREATE statement must include a PRIMARY KEY definition.<para/>
        /// If the table already exists, the CREATE statement is ignored.<para/>
        /// Creating your database tables with this method is not required to use the rest of the Butterfly framework (you can instead just load your schema from your existing database using <see cref="LoadSchemaAsync"/>.
        /// </summary>
        /// <param name="resourceFile"></param>
        /// <returns></returns>
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
        protected readonly List<Action<DataEventTransaction>> uncommittedTransactionListeners = new List<Action<DataEventTransaction>>();
        public IDisposable AddUncommittedTransactionListener(Action<DataEventTransaction> listener) => new ListItemDisposable<Action<DataEventTransaction>>(uncommittedTransactionListeners, listener);

        protected readonly List<Func<DataEventTransaction, Task>> asyncUncommittedTransactionListeners = new List<Func<DataEventTransaction, Task>>();
        public IDisposable AddUncommittedTransactionListener(Func<DataEventTransaction, Task> listener) => new ListItemDisposable<Func<DataEventTransaction, Task>>(asyncUncommittedTransactionListeners, listener);

        protected readonly List<Action<DataEventTransaction>> committedTransactionListeners = new List<Action<DataEventTransaction>>();
        public IDisposable OnNewCommittedTransaction(Action<DataEventTransaction> listener) => new ListItemDisposable<Action<DataEventTransaction>>(committedTransactionListeners, listener);

        protected readonly List<Func<DataEventTransaction, Task>> asyncCommittedTransactionListeners = new List<Func<DataEventTransaction, Task>>();
        public IDisposable AddCommittedTransactionListener(Func<DataEventTransaction, Task> listener) => new ListItemDisposable<Func<DataEventTransaction, Task>>(asyncCommittedTransactionListeners, listener);

        internal async Task ProcessDataEventTransaction(TransactionState transactionState, DataEventTransaction dataEventTransaction) {
            List<Task> tasks = new List<Task>();
            switch (transactionState) {
                case TransactionState.Uncommitted:
                    // Use ToArray() to avoid the collection being modified during the loop
                    foreach (var listener in this.uncommittedTransactionListeners.ToArray()) {
                        listener(dataEventTransaction);
                    }
                    // Use ToArray() to avoid the collection being modified during the loop
                    foreach (var listener in this.asyncUncommittedTransactionListeners.ToArray()) {
                        tasks.Add(listener(dataEventTransaction));
                    }
                    await Task.WhenAll(tasks);
                    break;
                case TransactionState.Committed:
                    // Use ToArray() to avoid the collection being modified during the loop
                    foreach (var listener in this.committedTransactionListeners.ToArray()) {
                        listener(dataEventTransaction);
                    }
                    // Use ToArray() to avoid the collection being modified during the loop
                    foreach (var listener in this.asyncCommittedTransactionListeners.ToArray()) {
                        tasks.Add(listener(dataEventTransaction));
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

        /// <summary>
        /// Executes the select statement of the DynamicQuery and returns a sequence of DataChange events starting an InitialBegin event, then an Insert event for each row, and then an InitialEnd event.
        /// </summary>
        /// <returns></returns>
        public async Task<DataEvent[]> GetInitialDataEventsAsync(string dataEventName, string[] keyFieldNames, SelectStatement selectStatement, dynamic statementParams = null) {
            logger.Debug($"GetInitialDataEvents():sql={selectStatement.Sql}");

            List<DataEvent> dataEvents = new List<DataEvent>();
            dataEvents.Add(new InitialBeginDataEvent(dataEventName, keyFieldNames));

            Dict[] rows = await this.SelectRowsAsync(selectStatement, statementParams);
            ChangeDataEvent[] changeDataEvents = rows.Select(x => new ChangeDataEvent(DataEventType.Initial, dataEventName, x)).ToArray();
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
            statement.ConfirmAllParamsUsed(statementParamsDict);
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
        public void SetDefaultValue(string fieldName, Func<object> getDefaultValue, string tableName = null) {
            if (tableName == null) {
                this.getDefaultValueByFieldName[fieldName] = getDefaultValue;
            }
            else {
                if (!this.Tables.TryGetValue(tableName, out Table table)) throw new Exception($"Invalid table name '{tableName}'");
                table.SetDefaultValue(fieldName, getDefaultValue);
            }
        }

        internal Dict ApplyDefaultValues(Table table, Dict values) {
            Dictionary<string, object> newValues = new Dict(values);
            foreach ((string fieldName, Func<object> getDefaultValue) in table.GetDefaultValueByFieldName) {
                FieldDef fieldDef = table.FindFieldDef(fieldName);
                if (fieldDef!=null && !newValues.ContainsKey(fieldDef.name)) newValues[fieldDef.name] = getDefaultValue();
            }
            foreach ((string fieldName, Func<object> getDefaultValue) in this.getDefaultValueByFieldName) {
                FieldDef fieldDef = table.FindFieldDef(fieldName);
                if (fieldDef != null && !newValues.ContainsKey(fieldDef.name)) newValues[fieldDef.name] = getDefaultValue();
            }
            return newValues;
        }

        public static object GetKeyValue(string[] fieldNames, Dict record) {
            if (fieldNames.Length == 1) {
                return record[fieldNames.First()];
            }
            else {
                StringBuilder sb = new StringBuilder();
                bool isFirst = true;
                foreach (var fieldName in fieldNames) {
                    if (isFirst) isFirst = false;
                    else sb.Append(";");
                    sb.Append(record[fieldName]);
                }
                return sb.ToString();
            }
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

        public DynamicViewSet CreateDynamicViewSet(Action<DataEventTransaction> listener, Func<DataEvent, bool> listenerDataEventFilter = null) {
            return new DynamicViewSet(this, listener, listenerDataEventFilter);
        }

        public DynamicViewSet CreateDynamicViewSet(Func<DataEventTransaction, Task> asyncListener, Func<DataEvent, bool> listenerDataEventFilter = null) {
            return new DynamicViewSet(this, asyncListener, listenerDataEventFilter);
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
