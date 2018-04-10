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

using Butterfly.Database.Event;
using Butterfly.Util;

using Dict = System.Collections.Generic.Dictionary<string, object>;

namespace Butterfly.Database {

    /// <inheritdoc/>
    /// <summary>
    /// Base class implementing <see cref="ITransaction"/>. New implementations will normally extend this class.
    /// </summary>
    public abstract class BaseTransaction : ITransaction {
        protected readonly BaseDatabase database;

        protected readonly List<KeyValueDataEvent> dataEvents = new List<KeyValueDataEvent>();

        public BaseTransaction(BaseDatabase database) {
            this.database = database;
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

        public async Task<object> InsertAsync(InsertStatement insertStatement, dynamic statementParams, bool ignoreIfDuplicate = false) {
            // Convert statementParams
            Dict statementParamsDict = insertStatement.ConvertParamsToDict(statementParams);
            this.database.PreprocessInput(insertStatement.TableRefs[0].table.Name, statementParamsDict);
            Dict defaultValuesDict = this.database.GetInsertDefaultValues(insertStatement.TableRefs[0].table);

            // Get the executable sql and params
            (string executableSql, Dict executableParams) = insertStatement.GetExecutableSqlAndParams(statementParamsDict, defaultValuesDict);

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
            if (insertStatement.TableRefs[0].table.AutoIncrementFieldName != null && getGeneratedId != null) {
                keyValue = getGeneratedId();
            }
            else {
                keyValue = BaseDatabase.GetKeyValue(insertStatement.TableRefs[0].table.Indexes[0].FieldNames, executableParams);
            }

            // Create data event
            this.dataEvents.Add(new KeyValueDataEvent(DataEventType.Insert, insertStatement.TableRefs[0].table.Name, keyValue));

            return keyValue;
        }

        protected abstract Task<Func<object>> DoInsertAsync(string executableSql, Dict executableParams, bool ignoreIfDuplicate);

        // Update methods
        public async Task<int> UpdateAsync(string updateStatement, dynamic vars) {
            UpdateStatement statement = new UpdateStatement(this.database, updateStatement);
            return await this.UpdateAsync(statement, vars);
        }

        public async Task<int> UpdateAsync(UpdateStatement updateStatement, dynamic vars) {
            // Convert statementParams
            Dict statementParamsDict = updateStatement.ConvertParamsToDict(vars);
            this.database.PreprocessInput(updateStatement.TableRefs[0].table.Name, statementParamsDict);

            // Determine keyValue
            (var whereIndex, var setRefs, var whereRefs) = updateStatement.GetWhereIndexSetRefsAndWhereRefs(this.database, statementParamsDict);

            // Get the executable sql and params
            (string executableSql, Dict executableParams) = updateStatement.GetExecutableSqlAndParams(statementParamsDict, setRefs, whereRefs);

            Dict fieldValues;
            if (whereIndex.IndexType == TableIndexType.Primary) {
                fieldValues = BaseStatement.RemapStatementParamsToFieldValues(statementParamsDict, whereRefs);
            }
            else {
                var selectValues = whereRefs.ToDictionary(x => x.fieldName, x => executableParams[x.fieldName]);
                fieldValues = await this.database.SelectRowAsync(updateStatement.TableRefs[0].table.Name, selectValues);
            }
            object keyValue = BaseDatabase.GetKeyValue(updateStatement.TableRefs[0].table.Indexes[0].FieldNames, fieldValues);

            // Execute update
            int count = await this.DoUpdateAsync(executableSql, executableParams);

            // Create data event
            this.dataEvents.Add(new KeyValueDataEvent(DataEventType.Update, updateStatement.TableRefs[0].table.Name, keyValue));

            return count;
        }

        protected abstract Task<int> DoUpdateAsync(string executableSql, Dict executableParams);

        // Delete methods
        public async Task<int> DeleteAsync(string deleteStatement, dynamic vars) {
            DeleteStatement statement = new DeleteStatement(this.database, deleteStatement);
            return await this.DeleteAsync(statement, vars);
        }

        public async Task<int> DeleteAsync(DeleteStatement deleteStatement, dynamic vars) {
            // Convert statementParams
            Dict statementParamsDict = deleteStatement.ConvertParamsToDict(vars);

            // Determine keyValue
            (var whereIndex, var whereRefs) = deleteStatement.GetWhereIndexAndWhereRefs(this.database, statementParamsDict);

            // Get the executable sql and params
            (string executableSql, Dict executableParams) = deleteStatement.GetExecutableSqlAndParams(statementParamsDict, whereRefs);

            Dict fieldValues;
            if (whereIndex.IndexType == TableIndexType.Primary) {
                fieldValues = BaseStatement.RemapStatementParamsToFieldValues(statementParamsDict, whereRefs);
            }
            else {
                var selectValues = whereRefs.ToDictionary(x => x.fieldName, x => executableParams[x.fieldName]);
                fieldValues = await this.database.SelectRowAsync(deleteStatement.TableRefs[0].table.Name, selectValues);
            }
            object keyValue = BaseDatabase.GetKeyValue(deleteStatement.TableRefs[0].table.Indexes[0].FieldNames, fieldValues);

            // Execute delete
            int count = await this.DoDeleteAsync(executableSql, executableParams);

            // Create data event
            this.dataEvents.Add(new KeyValueDataEvent(DataEventType.Delete, deleteStatement.TableRefs[0].table.Name, keyValue));

            return count;
        }

        protected abstract Task<int> DoDeleteAsync(string executableSql, Dict executableParams);

        public async Task<bool> Synchronize(string tableName, Dict[] existingRecords, Dict[] newRecords, string[] keyFieldNames = null) {
            if (!this.database.Tables.TryGetValue(tableName, out Table table)) throw new Exception($"Invalid table name '{tableName}'");

            bool changed = false;

            if (keyFieldNames == null) keyFieldNames = table.Indexes[0].FieldNames;

            List<object> existingIds = existingRecords.Select(x => BaseDatabase.GetKeyValue(keyFieldNames, x)).ToList();
            List<object> newIds = newRecords.Select(x => BaseDatabase.GetKeyValue(keyFieldNames, x)).ToList();

            for (int i = 0; i < existingIds.Count; i++) {
                int newIndex = newIds.IndexOf(existingIds[i]);
                int count = 0;
                if (newIndex == -1) {
                    count = await this.DeleteAsync(table.Name, existingIds[i]);
                }
                else if (!newRecords[newIndex].IsSame(existingRecords[i])) {
                    count = await this.UpdateAsync(table.Name, newRecords[newIndex]);
                }
                if (count > 0) changed = true;
            }

            for (int i = 0; i < newIds.Count; i++) {
                int existingIndex = existingIds.IndexOf(newIds[i]);
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
                this.database.ProcessDataEventTransaction(TransactionState.Uncommitted, dataEventTransaction);
            }
            this.DoCommit();
            if (dataEventTransaction != null) {
                this.database.ProcessDataEventTransaction(TransactionState.Committed, dataEventTransaction);
            }
        }

        protected abstract void DoCommit();

        public async Task CommitAsync() {
            DataEventTransaction dataEventTransaction = this.dataEvents.Count > 0 ? new DataEventTransaction(DateTime.Now, this.dataEvents.ToArray()) : null;
            if (dataEventTransaction!=null) {
                await this.database.ProcessDataEventTransactionAsync(TransactionState.Uncommitted, dataEventTransaction);
            }
            await this.DoCommitAsync();
            if (dataEventTransaction!=null) {
                await this.database.ProcessDataEventTransactionAsync(TransactionState.Committed, dataEventTransaction);
            }
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
