/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Data.Sqlite;
using NLog;

using Butterfly.Core.Database;

using Dict = System.Collections.Generic.Dictionary<string, object>;

namespace Butterfly.SQLite {

    /// <inheritdoc/>
    public class SQLiteTransaction : BaseTransaction {

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        protected SqliteConnection connection;
        protected Microsoft.Data.Sqlite.SqliteTransaction transaction;

        public SQLiteTransaction(SQLiteDatabase database) : base(database) {
        }

        public override void Begin() {
            SQLiteDatabase sqliteDatabase = this.database as SQLiteDatabase;
            this.connection = new SqliteConnection(sqliteDatabase.ConnectionString);
            this.connection.Open();
            this.transaction = this.connection.BeginTransaction();
        }

        public override async Task BeginAsync() {
            SQLiteDatabase sqliteDatabase = this.database as SQLiteDatabase;
            this.connection = new SqliteConnection(sqliteDatabase.ConnectionString);
            await this.connection.OpenAsync();
            this.transaction = this.connection.BeginTransaction();
        }

        protected override void DoCommit() {
            this.transaction.Commit();
        }

        protected override Task DoCommitAsync() {
            this.DoCommit();
            return Task.FromResult(0);
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

            bool isFirst = true;
            bool hasAutoIncrement = false;
            sb.Append($"CREATE TABLE {statement.TableName} (\r\n");
            foreach (var fieldDef in statement.FieldDefs) {
                if (isFirst) isFirst = false;
                else sb.Append(",\r\n");

                sb.Append(fieldDef.name);

                if (fieldDef.isAutoIncrement) {
                    sb.Append($" INTEGER PRIMARY KEY");
                    hasAutoIncrement = true;
                }
                else {
                    if (fieldDef.type == typeof(string)) {
                        sb.Append($" TEXT");
                    }
                    else if (fieldDef.type == typeof(int) || fieldDef.type == typeof(long)) {
                        sb.Append($" INTEGER");
                    }
                    else if (fieldDef.type == typeof(float) || fieldDef.type == typeof(double)) {
                        sb.Append($" REAL");
                    }
                    else if (fieldDef.type == typeof(DateTime)) {
                        sb.Append($" INTEGER");
                    }

                    if (!fieldDef.allowNull) sb.Append(" NOT NULL");
                }

            }

            if (!hasAutoIncrement) {
                sb.Append(",\r\n");
                sb.Append($" PRIMARY KEY ({string.Join(",", statement.Indexes[0].FieldNames)})");
            }
            sb.Append(")");
            return sb.ToString();
        }

        protected override async Task<Func<object>> DoInsertAsync(string executableSql, Dict executableParams, bool ignoreIfDuplicate) {
            try {
                InsertStatement statement = new InsertStatement(this.database, executableSql);
                bool hasAutoIncrement = statement.StatementFromRefs[0].table.AutoIncrementFieldName != null;

                var command = new SqliteCommand(executableSql, this.connection, this.transaction);
                if (executableParams != null) {
                    foreach (var keyValuePair in executableParams) {
                        command.Parameters.AddWithValue(keyValuePair.Key, keyValuePair.Value);
                    }
                }
                await command.ExecuteNonQueryAsync();
                if (hasAutoIncrement) {
                    return () => {
                        string sql = @"select last_insert_rowid()";
                        var lastInsertRowIdCommand = new SqliteCommand(sql, this.connection);
                        return (long)lastInsertRowIdCommand.ExecuteScalar();
                    };
                }
                else {
                    return null;
                }
            }
            catch (SqliteException e) {
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
            return this.DoExecuteAsync($"DELETE FROM {tableName}");
        }

        protected int DoExecute(string executableSql, Dict executableParams = null) {
            try {
                var command = new SqliteCommand(executableSql, this.connection, this.transaction);
                if (executableParams != null) {
                    foreach (var keyValuePair in executableParams) {
                        command.Parameters.AddWithValue(keyValuePair.Key, keyValuePair.Value);
                    }
                }
                return command.ExecuteNonQuery();
            }
            catch (SqliteException e) {
                throw new DatabaseException(e.Message);
            }
        }

        protected async Task<int> DoExecuteAsync(string executableSql, Dict executableParams = null) {
            try {
                var command = new SqliteCommand(executableSql, this.connection, this.transaction);
                if (executableParams != null) {
                    foreach (var keyValuePair in executableParams) {
                        command.Parameters.AddWithValue(keyValuePair.Key, keyValuePair.Value);
                    }
                }
                return await command.ExecuteNonQueryAsync();
            }
            catch (SqliteException e) {
                throw new DatabaseException(e.Message);
            }
        }

    }
}
