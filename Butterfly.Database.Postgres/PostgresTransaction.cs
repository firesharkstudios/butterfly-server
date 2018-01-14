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
using System.Text;
using System.Threading.Tasks;

using Npgsql;
using NLog;

using Dict = System.Collections.Generic.Dictionary<string, object>;

namespace Butterfly.Database.Postgres {

    /// <inheritdoc/>
    public class PostgresTransaction : BaseTransaction {

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        protected NpgsqlConnection connection;
        protected NpgsqlTransaction transaction;

        public PostgresTransaction(PostgresDatabase database) : base(database) {
        }

        public override void Begin() {
            PostgresDatabase postgresDatabase = this.database as PostgresDatabase;
            this.connection = new NpgsqlConnection(postgresDatabase.ConnectionString);
            this.connection.Open();
            this.transaction = this.connection.BeginTransaction();
        }

        public override async Task BeginAsync() {
            PostgresDatabase postgresDatabase = this.database as PostgresDatabase;
            this.connection = new NpgsqlConnection(postgresDatabase.ConnectionString);
            await this.connection.OpenAsync();
            this.transaction = this.connection.BeginTransaction();
        }

        protected override void DoCommit() {
            this.transaction.Commit();
        }

        protected override async Task DoCommitAsync() {
            await this.transaction.CommitAsync();
        }

        protected override void DoRollback() {
            this.transaction.Rollback();
        }

        public override void Dispose() {
            this.transaction.Dispose();
            this.connection.Dispose();
        }

        protected override bool DoCreate(CreateStatement statement) {
            string sql = BuildCreate(statement);
            this.DoExecute(sql);
            return false;
        }

        protected override async Task<bool> DoCreateAsync(CreateStatement statement) {
            string sql = BuildCreate(statement);
            await this.DoExecuteAsync(sql);
            return false;
        }

        /*
         * Example...
         * CREATE TABLE distributors (
         *  did    integer PRIMARY KEY DEFAULT nextval('serial'),
         *  name   varchar(40) NOT NULL CHECK (name <> '')
         * );
         */
        protected static string BuildCreate(CreateStatement statement) {
            StringBuilder sb = new StringBuilder();
            sb.Append($"CREATE TABLE {statement.TableName} (\r\n");
            foreach (var fieldDef in statement.FieldDefs) {
                sb.Append(fieldDef.name);

                if (fieldDef.isAutoIncrement) {
                    sb.Append($" SERIAL");
                }
                else if (fieldDef.type == typeof(string)) {
                    sb.Append($" VARCHAR({fieldDef.maxLength})");
                }
                else if (fieldDef.type == typeof(int)) {
                    sb.Append($" INTEGER");
                }
                else if (fieldDef.type == typeof(long)) {
                    sb.Append($" BIGINT");
                }
                else if (fieldDef.type == typeof(float)) {
                    sb.Append($" REAL");
                }
                else if (fieldDef.type == typeof(double)) {
                    sb.Append($" DOUBLE PRECISION");
                }
                else if (fieldDef.type == typeof(DateTime)) {
                    sb.Append($" TIMESTAMP");
                }

                if (!fieldDef.allowNull) sb.Append(" NOT");
                sb.Append(" NULL");

                sb.Append(",\r\n");
            }
            sb.Append($" PRIMARY KEY ({string.Join(",", statement.Indexes[0].FieldNames)})");
            sb.Append(")");
            return sb.ToString();
        }

        protected override async Task<Func<object>> DoInsertAsync(string executableSql, Dict executableParams, bool ignoreIfDuplicate) {
            try {
                InsertStatement statement = new InsertStatement(this.database, executableSql);
                bool hasAutoIncrement = statement.TableRefs[0].table.AutoIncrementFieldName != null;
                string newExecutableSql = hasAutoIncrement ? $"{executableSql} RETURNING {statement.TableRefs[0].table.AutoIncrementFieldName}" : executableSql; 

                var command = new NpgsqlCommand(newExecutableSql, this.connection);
                if (executableParams != null) {
                    foreach (var keyValuePair in executableParams) {
                        command.Parameters.AddWithValue(keyValuePair.Key, keyValuePair.Value);
                    }
                }
                if (hasAutoIncrement) {
                    object autoIncrementId = null;
                    using (var reader = await command.ExecuteReaderAsync()) {
                        while (reader.Read()) {
                            autoIncrementId = Convert.ToInt64(reader[0]);
                        }
                    }
                    return () => autoIncrementId;
                }
                else {
                    await command.ExecuteNonQueryAsync();
                    return null;
                }
            }
            catch (PostgresException e) {
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
                var command = new NpgsqlCommand(executableSql, this.connection);
                if (executableParams != null) {
                    foreach (var keyValuePair in executableParams) {
                        command.Parameters.AddWithValue(keyValuePair.Key, keyValuePair.Value);
                    }
                }
                return command.ExecuteNonQuery();
            }
            catch (PostgresException e) {
                throw new DatabaseException(e.Message);
            }
        }

        protected async Task<int> DoExecuteAsync(string executableSql, Dict executableParams = null) {
            try {
                var command = new NpgsqlCommand(executableSql, this.connection);
                if (executableParams != null) {
                    foreach (var keyValuePair in executableParams) {
                        command.Parameters.AddWithValue(keyValuePair.Key, keyValuePair.Value);
                    }
                }
                return await command.ExecuteNonQueryAsync();
            }
            catch (PostgresException e) {
                throw new DatabaseException(e.Message);
            }
        }

    }
}
