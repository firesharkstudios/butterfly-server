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
using System.Linq;
using System.Threading.Tasks;
using Butterfly.Core.Database;
using MySql.Data.MySqlClient;
using NLog;

using Dict = System.Collections.Generic.Dictionary<string, object>;

namespace Butterfly.MySql {

    /// <inheritdoc/>
    public class MySqlTransaction : BaseTransaction {

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        protected MySqlConnection connection;
        protected global::MySql.Data.MySqlClient.MySqlTransaction transaction;

        public MySqlTransaction(MySqlDatabase database) : base(database) {
        }

        public override void Begin() {
            MySqlDatabase mySqlDatabase = this.database as MySqlDatabase;
            this.connection = new MySqlConnection(mySqlDatabase.ConnectionString);
            this.connection.Open();
            this.transaction = this.connection.BeginTransaction();
        }

        public override async Task BeginAsync() {
            MySqlDatabase mySqlDatabase = this.database as MySqlDatabase;
            this.connection = new MySqlConnection(mySqlDatabase.ConnectionString);
            await this.connection.OpenAsync();
            this.transaction = await this.connection.BeginTransactionAsync();
        }

        protected override void DoCommit() {
            this.transaction.Commit();
        }

        protected override Task DoCommitAsync() {
            return this.transaction.CommitAsync();
        }

        protected override void DoRollback() {
            this.transaction.Rollback();
        }

        public override void Dispose() {
            this.transaction.Dispose();
            this.connection.Dispose();
        }

        protected override bool DoCreate(CreateStatement statement) {
            this.DoExecute(statement.Sql);
            return false;
        }

        protected override async Task<bool> DoCreateAsync(CreateStatement statement) {
            await this.DoExecuteAsync(statement.Sql);
            return false;
        }

        protected override async Task<Func<object>> DoInsertAsync(string executableSql, Dict executableParams, bool ignoreIfDuplicate) {
            try {
                using (MySqlCommand command = new MySqlCommand(executableSql, this.connection, this.transaction)) {
                    if (executableParams != null) {
                        MySqlParameter[] mySqlParams = executableParams.Select(x => new MySqlParameter(x.Key, x.Value)).ToArray();
                        command.Parameters.AddRange(mySqlParams);
                    }
                    await command.ExecuteNonQueryAsync();
                    return () => command.LastInsertedId;
                }
            }
            catch (MySqlException e) {
                if (e.Message.StartsWith("Duplicate entry")) {
                    throw new DuplicateKeyDatabaseException(e.Message);
                }
                else {
                    throw new DatabaseException(e.Message);
                }
            }
        }

        protected override Task<int> DoUpdateAsync(string executableSql, Dict executableParams) {
            return this.DoExecuteAsync(executableSql, executableParams);
        }

        protected override Task<int> DoDeleteAsync(string executableSql, Dict executableParams) {
            return this.DoExecuteAsync(executableSql, executableParams);
        }

        protected override Task DoTruncateAsync(string tableName) {
            return this.DoExecuteAsync($"TRUNCATE {tableName}");
        }

        protected int DoExecute(string executableSql, Dict executableParams = null) {
            try {
                using (MySqlCommand command = new MySqlCommand(executableSql, this.connection, this.transaction)) {
                    if (executableParams != null) {
                        MySqlParameter[] mySqlParams = executableParams.Select(keyValuePair => new MySqlParameter(keyValuePair.Key, keyValuePair.Value)).ToArray();
                        command.Parameters.AddRange(mySqlParams);
                    }
                    return command.ExecuteNonQuery();
                }
            }
            catch (MySqlException e) {
                throw new DatabaseException(e.Message);
            }
        }

        protected async Task<int> DoExecuteAsync(string executableSql, Dict executableParams = null) {
            try {
                using (MySqlCommand command = new MySqlCommand(executableSql, this.connection, this.transaction)) {
                if (executableParams != null) {
                    MySqlParameter[] mySqlParams = executableParams.Select(keyValuePair => new MySqlParameter(keyValuePair.Key, keyValuePair.Value)).ToArray();
                    command.Parameters.AddRange(mySqlParams);
                }
                return await command.ExecuteNonQueryAsync();
            }
            }
            catch (MySqlException e) {
                throw new DatabaseException(e.Message);
            }
        }

    }
}
