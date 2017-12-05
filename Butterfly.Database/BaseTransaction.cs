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
using System.Threading.Tasks;

using Butterfly.Database.Event;

using Dict = System.Collections.Generic.Dictionary<string, object>;

namespace Butterfly.Database {

    /// <summary>
    /// Base class implementing <see cref="ITransaction"/>. New implementations will normally extend this class.
    /// </summary>
    public abstract class BaseTransaction : ITransaction {
        protected readonly BaseDatabase database;

        protected readonly List<KeyValueDataEvent> dataEvents = new List<KeyValueDataEvent>();

        public BaseTransaction(BaseDatabase database) {
            this.database = database;
        }

        // Create methods
        public async Task<bool> CreateAsync(CreateStatement statement) {
            return await this.DoCreateAsync(statement);
        }

        protected abstract Task<bool> DoCreateAsync(CreateStatement statement);

        // Insert methods
        public async Task<object> InsertAsync(string statementSql, dynamic statementParams, bool ignoreIfDuplicate = false) {
            InsertStatement statement = new InsertStatement(this.database, statementSql);
            return await this.InsertAsync(statement, statementParams, ignoreIfDuplicate: ignoreIfDuplicate);
        }

        public async Task<object> InsertAsync(InsertStatement statement, dynamic statementParams, bool ignoreIfDuplicate = false) {
            // Convert statementParams
            Dict statementParamsDict = statement.ConvertParamsToDict(statementParams);
            Dict defaultValues = this.database.GetInsertDefaultValues(statement.TableRefs[0].table);

            // Get the executable sql and params
            (string executableSql, Dict executableParams) = statement.GetExecutableSqlAndParams(statementParamsDict, defaultValues);

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
            if (statement.TableRefs[0].table.AutoIncrementFieldName != null && getGeneratedId != null) {
                keyValue = getGeneratedId();
            }
            else {
                keyValue = BaseDatabase.GetKeyValue(statement.TableRefs[0].table.PrimaryIndex.FieldNames, executableParams);
            }

            // Create data event
            this.dataEvents.Add(new KeyValueDataEvent(DataEventType.Insert, statement.TableRefs[0].table.Name, keyValue));

            return keyValue;
        }

        protected abstract Task<Func<object>> DoInsertAsync(string executableSql, Dict executableParams, bool ignoreIfDuplicate);

        // Update methods
        public async Task<int> UpdateAsync(string statementSql, dynamic statementParams) {
            UpdateStatement statement = new UpdateStatement(this.database, statementSql);
            return await this.UpdateAsync(statement, statementParams);
        }

        public async Task<int> UpdateAsync(UpdateStatement statement, dynamic statementParams) {
            // Convert statementParams
            Dict statementParamsDict = statement.ConvertParamsToDict(statementParams);

            // Determine keyValue
            var fieldValues = Statement.RemapStatementParamsToFieldValues(statementParamsDict, statement.WhereRefs);
            object keyValue = BaseDatabase.GetKeyValue(statement.TableRefs[0].table.PrimaryIndex.FieldNames, fieldValues);

            // Get the executable sql and params
            (string executableSql, Dict executableParams) = statement.GetExecutableSqlAndParams(statementParamsDict);

            // Execute update
            int count = await this.DoUpdateAsync(executableSql, executableParams);

            // Create data event
            this.dataEvents.Add(new KeyValueDataEvent(DataEventType.Update, statement.TableRefs[0].table.Name, keyValue));

            return count;
        }

        protected abstract Task<int> DoUpdateAsync(string executableSql, Dict executableParams);

        // Delete methods
        public async Task<int> DeleteAsync(string sql, dynamic statementParams) {
            DeleteStatement statement = new DeleteStatement(this.database, sql);
            return await this.DeleteAsync(statement, statementParams);
        }

        public async Task<int> DeleteAsync(DeleteStatement statement, dynamic statementParams) {
            // Convert statementParams
            Dict statementParamsDict = statement.ConvertParamsToDict(statementParams);

            // Determine keyValue
            var fieldValues = Statement.RemapStatementParamsToFieldValues(statementParamsDict, statement.WhereRefs);
            object keyValue = BaseDatabase.GetKeyValue(statement.TableRefs[0].table.PrimaryIndex.FieldNames, fieldValues);

            // Get the executable sql and params
            (string executableSql, Dict executableParams) = statement.GetExecutableSqlAndParams(statementParamsDict);

            // Execute delete
            int count = await this.DoDeleteAsync(executableSql, executableParams);

            // Create data event
            this.dataEvents.Add(new KeyValueDataEvent(DataEventType.Delete, statement.TableRefs[0].table.Name, keyValue));

            return count;
        }

        protected abstract Task<int> DoDeleteAsync(string executableSql, Dict executableParams);


        // Truncate methods
        public async Task TruncateAsync(string tableName) {
            await this.DoTruncateAsync(tableName);
        }

        protected abstract Task DoTruncateAsync(string tableName);

        public abstract Task BeginAsync();


        // Commit methods
        public async Task CommitAsync() {
            DataEventTransaction dataEventTransaction = this.dataEvents.Count > 0 ? new DataEventTransaction(DateTime.Now, this.dataEvents.ToArray()) : null;
            if (dataEventTransaction!=null) {
                await this.database.ProcessDataEventTransaction(TransactionState.Uncommitted, dataEventTransaction);
            }
            await this.DoCommit();
            if (dataEventTransaction!=null) {
                await this.database.ProcessDataEventTransaction(TransactionState.Committed, dataEventTransaction);
            }
        }

        protected abstract Task DoCommit();

        // Rollback methods
        public void Rollback() {
            this.DoRollback();
        }

        protected abstract void DoRollback();


        // Dispose methods
        public abstract void Dispose();

    }
}
