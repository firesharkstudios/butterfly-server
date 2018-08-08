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

using Butterfly.Core.Database.Dynamic;
using Butterfly.Core.Database.Event;
using Butterfly.Core.Util;

using Dict = System.Collections.Generic.Dictionary<string, object>;

namespace Butterfly.Core.Database {

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
            this.LoadSchema();
        }

        /// <summary>
        /// Gets or sets the connection string
        /// </summary>
        /// <value>
        /// The connection string
        /// </value>
        public string ConnectionString {
            get;
            protected set;
        }

        public Dictionary<string, Table> Tables => this.tableByName;

        public abstract bool CanJoin { get; }

        public abstract bool CanFieldAlias { get; }

        public int SelectCount {
            get;
            internal set;
        }

        public int TransactionCount {
            get;
            internal set;
        }

        public int InsertCount {
            get;
            internal set;
        }

        public int UpdateCount {
            get;
            internal set;
        }

        public int DeleteCount {
            get;
            internal set;
        }


        public void CreateFromResourceFile(Assembly assembly, string resourceFile) {
            //logger.Debug($"CreateFromResourceFile():resourceNames={string.Join(",", assembly.GetManifestResourceNames())}");
            string sql = FileX.LoadResourceAsText(assembly, resourceFile);
            this.CreateFromText(sql);
        }

        public async Task CreateFromResourceFileAsync(Assembly assembly, string resourceFile) {
            //logger.Debug($"CreateFromResourceFileAsync():resourceNames={string.Join(",", assembly.GetManifestResourceNames())}");
            string sql = await FileX.LoadResourceAsTextAsync(assembly, resourceFile);
            await this.CreateFromTextAsync(sql);
        }

        public void CreateFromText(string createStatements) {
            logger.Trace($"CreateFromText():createStatements={createStatements}");
            var noCommentSql = SQL_COMMENT.Replace(createStatements, "");
            var sqlParts = noCommentSql.Split(';').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x));

            List<string> tableSchemasToLoad = new List<string>();
            using (var transaction = this.BeginTransaction() as BaseTransaction) {
                foreach (var sqlPart in sqlParts) {
                    if (!string.IsNullOrWhiteSpace(sqlPart)) {
                        CreateStatement statement = this.CreateStatement(sqlPart);
                        if (!this.Tables.Keys.Contains(statement.TableName)) {
                            bool tableSchemaLoaded = transaction.Create(statement);
                            if (!tableSchemaLoaded) {
                                tableSchemasToLoad.Add(statement.TableName);
                            }
                        }
                    }
                }
                transaction.Commit();
            }

            foreach (var tableName in tableSchemasToLoad) {
                Table table = this.LoadTableSchema(tableName);
                this.tableByName[table.Name] = table;
            }
        }

        public async Task CreateFromTextAsync(string createStatements) {
            logger.Trace($"CreateFromTextAsync():createStatements={createStatements}");
            var noCommentSql = SQL_COMMENT.Replace(createStatements, "");
            var sqlParts = noCommentSql.Split(';').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x));

            List<string> tableSchemasToLoad = new List<string>();
            using (var transaction = await this.BeginTransactionAsync() as BaseTransaction) {
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
                Table table = this.LoadTableSchema(tableName);
                this.tableByName[table.Name] = table;
            }
        }

        protected virtual CreateStatement CreateStatement(string sql) {
            return new CreateStatement(sql);
        }

        protected abstract void LoadSchema();

        protected abstract Table LoadTableSchema(string tableName);

        // Manage data event transaction listeners
        protected readonly List<DataEventTransactionListener> uncommittedTransactionListeners = new List<DataEventTransactionListener>();
        public IDisposable OnNewUncommittedTransaction(Action<DataEventTransaction> listener) => new ListItemDisposable<DataEventTransactionListener>(uncommittedTransactionListeners, new DataEventTransactionListener(listener));
        public IDisposable OnNewUncommittedTransaction(Func<DataEventTransaction, Task> listener) => new ListItemDisposable<DataEventTransactionListener>(uncommittedTransactionListeners, new DataEventTransactionListener(listener));

        protected readonly List<DataEventTransactionListener> committedTransactionListeners = new List<DataEventTransactionListener>();
        public IDisposable OnNewCommittedTransaction(Action<DataEventTransaction> listener) => new ListItemDisposable<DataEventTransactionListener>(committedTransactionListeners, new DataEventTransactionListener(listener));
        public IDisposable OnNewCommittedTransaction(Func<DataEventTransaction, Task> listener) => new ListItemDisposable<DataEventTransactionListener>(committedTransactionListeners, new DataEventTransactionListener(listener));

        internal void PostDataEventTransaction(TransactionState transactionState, DataEventTransaction dataEventTransaction) {
            // Use ToArray() to avoid the collection being modified during the loop
            DataEventTransactionListener[] listeners = transactionState == TransactionState.Uncommitted ? this.uncommittedTransactionListeners.ToArray() : this.committedTransactionListeners.ToArray();

            listeners.Where(x => x.listener != null).AsParallel().ForAll(x => x.listener(dataEventTransaction));
            Task[] tasks = listeners.Where(x => x.listenerAsync != null).Select(x => x.listenerAsync(dataEventTransaction)).ToArray();
            Task.WaitAll(tasks.ToArray());
        }

        internal async Task PostDataEventTransactionAsync(TransactionState transactionState, DataEventTransaction dataEventTransaction) {
            // Use ToArray() to avoid the collection being modified during the loop
            DataEventTransactionListener[] listeners = transactionState == TransactionState.Uncommitted ? this.uncommittedTransactionListeners.ToArray() : this.committedTransactionListeners.ToArray();

            listeners.Where(x => x.listener != null).AsParallel().ForAll(x => x.listener(dataEventTransaction));
            Task[] tasks = listeners.Where(x => x.listenerAsync != null).Select(x => x.listenerAsync(dataEventTransaction)).ToArray();
            await Task.WhenAll(tasks.ToArray());
        }

        internal async Task<DataEventTransaction> GetInitialDataEventTransactionAsync(string statementSql, dynamic statementParams = null) {
            SelectStatement statement = new SelectStatement(this, statementSql);
            DataEvent[] initialDataEvents = await this.GetInitialDataEventsAsync(statement.StatementFromRefs[0].table.Name, statement.StatementFromRefs[0].table.Indexes[0].FieldNames, statement, statementParams);
            return new DataEventTransaction(DateTime.Now, initialDataEvents);
        }

        internal async Task<DataEvent[]> GetInitialDataEventsAsync(string dataEventName, string[] keyFieldNames, SelectStatement selectStatement, dynamic statementParams = null) {
            logger.Debug($"GetInitialDataEvents():sql={selectStatement.Sql}");

            List<DataEvent> dataEvents = new List<DataEvent>();
            dataEvents.Add(new InitialBeginDataEvent(dataEventName, keyFieldNames));

            Dict[] rows = await this.SelectRowsAsync(selectStatement, statementParams);
            RecordDataEvent[] changeDataEvents = rows.Select(x => new RecordDataEvent(DataEventType.Initial, dataEventName, GetKeyValue(keyFieldNames, x), x)).ToArray();
            dataEvents.AddRange(changeDataEvents);

            return dataEvents.ToArray();
        }

        public async Task<T> SelectValueAsync<T>(string sql, dynamic vars = null, T defaultValue = default(T)) {
            Dict row = await this.SelectRowAsync(sql, vars);
            if (row == null || !row.TryGetValue(row.Keys.First(), out object value) || value==null) return defaultValue;

            return (T)Convert.ChangeType(value, typeof(T));
        }

        public async Task<Dict> SelectRowAsync(string statementSql, dynamic vars = null) {
            Dict[] rows = await this.SelectRowsAsync(statementSql, vars: vars, overrideLimit: 1);
            if (rows.Length == 0) return null;
            else if (rows.Length > 1) throw new Exception("SelectRow returned more than one row");
            return rows.First();
        }

        public async Task<Dict[]> SelectRowsAsync(string statementSql, dynamic vars = null, int overrideLimit = -1) {
            SelectStatement statement = new SelectStatement(this, statementSql, overrideLimit);
            return await this.SelectRowsAsync(statement, vars);
        }

        internal Task<Dict[]> SelectRowsAsync(SelectStatement statement, dynamic vars) {
            Dict varsDict = statement.ConvertParamsToDict(vars);
            (string executableSql, Dict executableParams) = statement.GetExecutableSqlAndParams(varsDict);
            this.SelectCount++;
            return this.DoSelectRowsAsync(executableSql, executableParams);
        }

        protected abstract Task<Dict[]> DoSelectRowsAsync(string executableSql, Dict executableParams);

        public async Task<T> QueryValueAsync<T>(string storedProcedureName, dynamic vars = null, T defaultValue = default(T)) {
            Dict row = await this.QueryRowAsync(storedProcedureName, vars);
            if (row == null || !row.TryGetValue(row.Keys.First(), out object value) || value == null) return defaultValue;

            return (T)Convert.ChangeType(value, typeof(T));
        }

        public async Task<Dict> QueryRowAsync(string storedProcedureName, dynamic vars = null) {
            Dict[] rows = await this.QueryRowsAsync(storedProcedureName, vars);
            if (rows.Length == 0) return null;
            else if (rows.Length > 1) throw new Exception("QueryRow returned more than one row");
            return rows.First();
        }

        public Task<Dict[]> QueryRowsAsync(string storedProcedureName, dynamic vars = null) {
            Dict executableParams;

            // If statementParams is null, return empty dictionary
            if (vars == null) {
                executableParams = new Dict();
            }

            // If statementParams is already a dictionary, return the dictionary
            else if (vars is Dict d) {
                executableParams = new Dict(d);
            }

            // Otherwise, convert statementParams to a dictionary
            else {
                executableParams = DynamicX.ToDictionary(vars);
            }

            return this.DoQueryRowsAsync(storedProcedureName, executableParams);
        }

        protected abstract Task<Dict[]> DoQueryRowsAsync(string storedProcedureName, Dict executableParams);

        public async Task<T> InsertAndCommitAsync<T>(string insertStatement, dynamic vars, bool ignoreIfDuplicate = false) {
            T result;
            using (var transaction = await this.BeginTransactionAsync()) {
                result = await transaction.InsertAsync<T>(insertStatement, vars, ignoreIfDuplicate);
                await transaction.CommitAsync();
            }
            return result;
        }

        public async Task<int> UpdateAndCommitAsync(string updateStatement, dynamic vars) {
            int count;
            using (var transaction = await this.BeginTransactionAsync()) {
                count = await transaction.UpdateAsync(updateStatement, vars);
                await transaction.CommitAsync();
            }
            return count;
        }

        public async Task<int> DeleteAndCommitAsync(string deleteStatement, dynamic vars) {
            int count;
            using (var transaction = await this.BeginTransactionAsync()) {
                count = await transaction.DeleteAsync(deleteStatement, vars);
                await transaction.CommitAsync();
            }
            return count;
        }

        public ITransaction BeginTransaction() {
            var transaction = this.CreateTransaction();
            transaction.Begin();
            return transaction;
        }

        public async Task<ITransaction> BeginTransactionAsync() {
            var transaction = this.CreateTransaction();
            await transaction.BeginAsync();
            return transaction;
        }

        protected abstract BaseTransaction CreateTransaction();

        protected readonly Dictionary<string, Func<string, object>> getDefaultValueByFieldName = new Dictionary<string, Func<string, object>>();

        public void SetDefaultValue(string fieldName, Func<string, object> getValue, string tableName = null) {
            if (tableName == null) {
                this.getDefaultValueByFieldName[fieldName] = getValue;
            }
            else {
                if (!this.Tables.TryGetValue(tableName, out Table table)) throw new Exception($"Invalid table name '{tableName}'");
                table.SetDefaultValue(fieldName, getValue);
            }
        }

        internal Dict GetDefaultValues(Table table) {
            Dictionary<string, object> defaultValues = new Dict();
            foreach ((string fieldName, Func<string, object> getDefaultValue) in table.GetDefaultValueByFieldName) {
                TableFieldDef fieldDef = table.FindFieldDef(fieldName);
                if (fieldDef!=null && !defaultValues.ContainsKey(fieldDef.name)) defaultValues[fieldDef.name] = getDefaultValue(table.Name);
            }
            foreach ((string fieldName, Func<string, object> getDefaultValue) in this.getDefaultValueByFieldName) {
                TableFieldDef fieldDef = table.FindFieldDef(fieldName);
                if (fieldDef != null && !defaultValues.ContainsKey(fieldDef.name)) defaultValues[fieldDef.name] = getDefaultValue(table.Name);
            }
            return defaultValues;
        }

        protected readonly Dictionary<string, Func<string, object>> getOverrideValueByFieldName = new Dictionary<string, Func<string, object>>();

        public void SetOverrideValue(string fieldName, Func<string, object> getValue, string tableName = null) {
            if (tableName == null) {
                this.getOverrideValueByFieldName[fieldName] = getValue;
            }
            else {
                if (!this.Tables.TryGetValue(tableName, out Table table)) throw new Exception($"Invalid table name '{tableName}'");
                table.SetOverrideValue(fieldName, getValue);
            }
        }

        internal Dict GetOverrideValues(Table table) {
            Dictionary<string, object> overrideValues = new Dict();
            foreach ((string fieldName, Func<string, object> getValue) in table.GetOverrideValueByFieldName) {
                TableFieldDef fieldDef = table.FindFieldDef(fieldName);
                if (fieldDef != null && !overrideValues.ContainsKey(fieldDef.name)) overrideValues[fieldDef.name] = getValue(table.Name);
            }
            foreach ((string fieldName, Func<string, object> getValue) in this.getOverrideValueByFieldName) {
                TableFieldDef fieldDef = table.FindFieldDef(fieldName);
                if (fieldDef != null && !overrideValues.ContainsKey(fieldDef.name)) overrideValues[fieldDef.name] = getValue(table.Name);
            }
            return overrideValues;
        }

        protected readonly List<Action<string, Dict>> inputPreprocessors = new List<Action<string, Dict>>();

        public void AddInputPreprocessor(Action<string, Dict> inputPreprocessor) {
            this.inputPreprocessors.Add(inputPreprocessor);
        }

        internal void PreprocessInput(string tableName, Dict input) {
            foreach (var inputPreprocessor in this.inputPreprocessors) {
                inputPreprocessor(tableName, input);
            }
        }

        public static Action<string, Dict> RemapTypeInputPreprocessor<T>(Func<T, object> convert) {
            return (tableName, input) => {
                foreach (var pair in input.ToArray()) {
                    if (pair.Value is T) {
                        input[pair.Key] = convert((T)pair.Value);
                    }
                }
            };
        }

        public static Action<string, Dict> CopyFieldValueInputPreprocessor(string token, string sourceFieldName) {
            return (tableName, input) => {
                foreach (var pair in input.ToArray()) {
                    string stringValue = pair.Value as string;
                    if (stringValue == token) {
                        input[pair.Key] = input[sourceFieldName];
                    }
                }
            };
        }

        protected readonly static Regex PARSE_TYPE = new Regex(@"^(?<type>.+?)(?<maxLengthWithParens>\(\d+\))?$");

        public static (Type, int) ConvertMySqlType(string text) {
            Match match = PARSE_TYPE.Match(text);
            if (!match.Success) throw new Exception($"Could not parse SQL type '{text}'");

            string typeText = match.Groups["type"].Value;

            Type type;
            if (typeText.EndsWith("CHAR", StringComparison.OrdinalIgnoreCase) || typeText.EndsWith("TEXT", StringComparison.OrdinalIgnoreCase)) {
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

        internal static object GetKeyValue(string[] fieldNames, Dict record, bool throwErrorIfMissingKeyField = true) {
            StringBuilder sb = new StringBuilder();
            bool isFirst = true;
            foreach (var fieldName in fieldNames) {
                if (isFirst) isFirst = false;
                else sb.Append(";");

                if (!record.ContainsKey(fieldName)) {
                    if (throwErrorIfMissingKeyField) throw new Exception($"Could not get key field '{fieldName}' to build key value");
                    return null;
                }
                else {
                    sb.Append(record[fieldName]);
                }
            }
            return sb.ToString();
        }

        internal static Dict ParseKeyValue(object keyValue, string[] keyFieldNames) {
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

    public class UnableToConnectDatabaseException : DatabaseException {
        public UnableToConnectDatabaseException(string message) : base(message) {
        }
    }

}
