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
using System.Threading.Tasks;

using Butterfly.Core.Database.Event;
using Butterfly.Core.Util;

using Dict = System.Collections.Generic.Dictionary<string, object>;

namespace Butterfly.Core.Database {

    /// <inheritdoc/>
    /// <summary>
    /// Base class implementing <see cref="ITransaction"/>.
    /// </summary>
    public abstract class BaseTransaction : ITransaction {
        protected readonly BaseDatabase database;

        protected readonly List<KeyValueDataEvent> dataEvents = new List<KeyValueDataEvent>();
        protected readonly List<Func<Task>> onCommits = new List<Func<Task>>();

        public BaseTransaction(BaseDatabase database) {
            this.database = database;
            this.database.TransactionCount++;
        }

        public IDatabase Database => this.database;

        // Create methods
        public bool Create(CreateStatement statement) {
            return this.DoCreate(statement);
        }

        protected abstract bool DoCreate(CreateStatement statement);

        public async Task<bool> CreateAsync(CreateStatement statement) {
            return await this.DoCreateAsync(statement);
        }

        protected abstract Task<bool> DoCreateAsync(CreateStatement statement);

        // Insert methods
        public async Task<T> InsertAsync<T>(string insertStatement, dynamic vars, bool ignoreIfDuplicate = false) {
            InsertStatement statement = new InsertStatement(this.database, insertStatement);
            object result = await this.InsertAsync(statement, vars, ignoreIfDuplicate: ignoreIfDuplicate);
            return (T)Convert.ChangeType(result, typeof(T));
        }

        public async Task<object> InsertAsync(InsertStatement insertStatement, dynamic vars, bool ignoreIfDuplicate = false) {
            // Convert statementParams
            Dict varsDict = insertStatement.ConvertParamsToDict(vars);
            this.database.PreprocessInput(insertStatement.StatementFromRefs[0].table.Name, varsDict);
            Dict varsOverrides = this.database.GetOverrideValues(insertStatement.StatementFromRefs[0].table);
            varsDict.UpdateFrom(varsOverrides);
            Dict varsDefaults = this.database.GetDefaultValues(insertStatement.StatementFromRefs[0].table);

            // Get the executable sql and params
            (string executableSql, Dict executableParams) = insertStatement.GetExecutableSqlAndParams(varsDict, varsDefaults);

            // Execute insert and return getGenerateId lambda
            Func<object> getGeneratedId;
            try {
                getGeneratedId = await this.DoInsertAsync(executableSql, executableParams, ignoreIfDuplicate);
            }
            catch (DuplicateKeyDatabaseException) {
                if (ignoreIfDuplicate) return null;
                throw;
            }

            // Determine keyValue (either keyValue is from a generated id or was included in the statement params)
            object keyValue;
            if (insertStatement.StatementFromRefs[0].table.AutoIncrementFieldName != null && getGeneratedId != null) {
                keyValue = getGeneratedId();
            }
            else {
                keyValue = BaseDatabase.GetKeyValue(insertStatement.StatementFromRefs[0].table.Indexes[0].FieldNames, executableParams);
            }

            // Create data event
            this.dataEvents.Add(new KeyValueDataEvent(DataEventType.Insert, insertStatement.StatementFromRefs[0].table.Name, keyValue));

            this.database.InsertCount++;

            return keyValue;
        }

        protected abstract Task<Func<object>> DoInsertAsync(string executableSql, Dict executableParams, bool ignoreIfDuplicate);

        // Update methods
        public async Task<int> UpdateAsync(string updateStatement, dynamic vars) {
            UpdateStatement statement = new UpdateStatement(this.database, updateStatement);
            return await this.UpdateAsync(statement, vars);
        }

        public async Task<int> UpdateAsync(UpdateStatement updateStatement, dynamic vars) {
            Dict varsDict = updateStatement.ConvertParamsToDict(vars);
            Dict varsOverrides = this.database.GetOverrideValues(updateStatement.StatementFromRefs[0].table);
            varsDict.UpdateFrom(varsOverrides);
            this.database.PreprocessInput(updateStatement.StatementFromRefs[0].table.Name, varsDict);

            (var whereIndex, var setRefs, var whereRefs) = updateStatement.GetWhereIndexSetRefsAndWhereRefs(this.database, varsDict);
            (string executableSql, Dict executableParams) = updateStatement.GetExecutableSqlAndParams(varsDict, setRefs, whereRefs);

            object keyValue = await this.GetKeyValue(whereIndex, varsDict, executableParams, whereRefs, updateStatement.StatementFromRefs[0].table.Indexes[0], updateStatement.StatementFromRefs[0].table.Name);

            int count;
            if (keyValue == null) {
                count = 0;
            }
            else {
                count = await this.DoUpdateAsync(executableSql, executableParams);

                this.dataEvents.Add(new KeyValueDataEvent(DataEventType.Update, updateStatement.StatementFromRefs[0].table.Name, keyValue));

                this.database.UpdateCount++;
            }

            return count;
        }

        protected abstract Task<int> DoUpdateAsync(string executableSql, Dict executableParams);

        // Delete methods
        public async Task<int> DeleteAsync(string deleteStatement, dynamic vars) {
            DeleteStatement statement = new DeleteStatement(this.database, deleteStatement);
            return await this.DeleteAsync(statement, vars);
        }

        public async Task<int> DeleteAsync(DeleteStatement deleteStatement, dynamic vars) {
            Dict varsDict = deleteStatement.ConvertParamsToDict(vars);

            (var whereIndex, var whereRefs) = deleteStatement.GetWhereIndexAndWhereRefs(this.database, varsDict);

            (string executableSql, Dict executableParams) = deleteStatement.GetExecutableSqlAndParams(varsDict, whereRefs);

            object keyValue = await this.GetKeyValue(whereIndex, varsDict, executableParams, whereRefs, deleteStatement.StatementFromRefs[0].table.Indexes[0], deleteStatement.StatementFromRefs[0].table.Name);

            int count;
            if (keyValue == null) {
                count = 0;
            }
            else {
                count = await this.DoDeleteAsync(executableSql, executableParams);

                this.dataEvents.Add(new KeyValueDataEvent(DataEventType.Delete, deleteStatement.StatementFromRefs[0].table.Name, keyValue));

                this.database.DeleteCount++;
            }

            return count;
        }

        protected abstract Task<int> DoDeleteAsync(string executableSql, Dict executableParams);

        protected async Task<object> GetKeyValue(TableIndex whereIndex, Dict varsDict, Dict executableParams, StatementEqualsRef[] whereRefs, TableIndex primaryIndex, string tableName) {
            Dict fieldValues;
            if (whereIndex.IndexType == TableIndexType.Primary) {
                fieldValues = BaseStatement.RemapStatementParamsToFieldValues(varsDict, whereRefs);
            }
            else {
                // This extra select to convert from a non-primary key index to a primary key index is unfortunate
                var selectValues = whereRefs.ToDictionary(x => x.fieldName, x => executableParams[x.fieldName]);
                fieldValues = await this.database.SelectRowAsync($"SELECT {string.Join(",", primaryIndex.FieldNames)} FROM {tableName}", selectValues);
            }
            if (fieldValues == null) {
                return null;
            }
            else {
                return BaseDatabase.GetKeyValue(primaryIndex.FieldNames, fieldValues);
            }
        }

        public async Task<bool> SynchronizeAsync(string tableName, Dict[] existingRecords, Dict[] newRecords, Func<Dict, dynamic> getDeleteKey, string[] keyFieldNames = null) {
            if (!this.database.Tables.TryGetValue(tableName, out Table table)) throw new Exception($"Invalid table name '{tableName}'");

            bool changed = false;

            if (keyFieldNames == null) keyFieldNames = table.Indexes[0].FieldNames;

            List<object> existingIds = existingRecords.Select(x => BaseDatabase.GetKeyValue(keyFieldNames, x)).ToList();
            List<object> newIds = newRecords.Select(x => BaseDatabase.GetKeyValue(keyFieldNames, x, throwErrorIfMissingKeyField: false)).ToList();

            for (int i = 0; i < existingIds.Count; i++) {
                int newIndex = newIds.IndexOf(existingIds[i]);
                int count = 0;
                if (newIndex == -1) {
                    var deleteKey = getDeleteKey(existingRecords[i]);
                    count = await this.DeleteAsync(table.Name, deleteKey);
                }
                else if (!newRecords[newIndex].IsSame(existingRecords[i])) {
                    count = await this.UpdateAsync(table.Name, newRecords[newIndex]);
                }
                if (count > 0) changed = true;
            }

            for (int i = 0; i < newIds.Count; i++) {
                int existingIndex = newIds[i]==null ? -1 : existingIds.IndexOf(newIds[i]);
                if (existingIndex == -1) {
                    await this.InsertAsync<object>(table.Name, newRecords[i]);
                    changed = true;
                }
            }

            return changed;
        }

        // Truncate methods
        public async Task TruncateAsync(string tableName) {
            await this.DoTruncateAsync(tableName);
        }

        protected abstract Task DoTruncateAsync(string tableName);

        // Begin methods
        public abstract void Begin();

        public abstract Task BeginAsync();

        // Commit methods
        public void Commit() {
            DataEventTransaction dataEventTransaction = this.dataEvents.Count > 0 ? new DataEventTransaction(DateTime.Now, this.dataEvents.ToArray()) : null;
            if (dataEventTransaction != null) {
                this.database.PostDataEventTransaction(TransactionState.Uncommitted, dataEventTransaction);
            }
            this.DoCommit();
            if (dataEventTransaction != null) {
                this.database.PostDataEventTransaction(TransactionState.Committed, dataEventTransaction);
            }
        }

        protected abstract void DoCommit();

        public async Task CommitAsync() {
            DataEventTransaction dataEventTransaction = this.dataEvents.Count > 0 ? new DataEventTransaction(DateTime.Now, this.dataEvents.ToArray()) : null;
            if (dataEventTransaction!=null) {
                await this.database.PostDataEventTransactionAsync(TransactionState.Uncommitted, dataEventTransaction);
            }
            await this.DoCommitAsync();
            if (dataEventTransaction!=null) {
                await this.database.PostDataEventTransactionAsync(TransactionState.Committed, dataEventTransaction);
            }

            foreach (var onCommit in this.onCommits) {
                await onCommit();
            }
        }

        public void OnCommit(Func<Task> onCommit) {
            this.onCommits.Add(onCommit);
        }

        protected abstract Task DoCommitAsync();

        // Rollback methods
        public void Rollback() {
            this.DoRollback();
        }

        protected abstract void DoRollback();


        // Dispose methods
        public abstract void Dispose();

    }
}
