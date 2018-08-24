/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Linq;
using System.Threading.Tasks;

using MySql.Data.MySqlClient;
using NLog;

using Butterfly.Core.Database;

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
