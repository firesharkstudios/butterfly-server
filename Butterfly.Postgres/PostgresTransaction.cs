/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Text;
using System.Threading.Tasks;

using Npgsql;
using NLog;

using Butterfly.Core.Database;

using Dict = System.Collections.Generic.Dictionary<string, object>;
using NpgsqlTypes;

namespace Butterfly.Postgres
{

	/// <inheritdoc/>
	public class PostgresTransaction : BaseTransaction
	{

		private static readonly Logger logger = LogManager.GetCurrentClassLogger();

		protected NpgsqlConnection connection;
		protected NpgsqlTransaction transaction;

		public PostgresTransaction(PostgresDatabase database) : base(database)
		{
		}

		public override void Begin()
		{
			PostgresDatabase postgresDatabase = this.database as PostgresDatabase;
			this.connection = new NpgsqlConnection(postgresDatabase.ConnectionString);
			this.connection.Open();
			this.transaction = this.connection.BeginTransaction();
		}

		public override async Task BeginAsync()
		{
			PostgresDatabase postgresDatabase = this.database as PostgresDatabase;
			this.connection = new NpgsqlConnection(postgresDatabase.ConnectionString);
			await this.connection.OpenAsync();
			this.transaction = this.connection.BeginTransaction();
		}

		protected override void DoCommit()
		{
			this.transaction.Commit();
		}

		protected override async Task DoCommitAsync()
		{
			await this.transaction.CommitAsync();
		}

		protected override void DoRollback()
		{
			this.transaction.Rollback();
		}

		public override void Dispose()
		{
			this.transaction.Dispose();
			this.connection.Dispose();
		}

		protected override bool DoCreate(CreateStatement statement)
		{
			var result = ExecuteCommand<int>(c =>
			{
				return c.ExecuteNonQuery();
			}, statement.Sql);
			return false;
		}

		protected override async Task<bool> DoCreateAsync(CreateStatement statement)
		{
			var result = await ExecuteCommandAsync<int>(async c =>
			{
				return await c.ExecuteNonQueryAsync();
			}, statement.Sql);
			return false;
		}
		
		protected override async Task<Func<object>> DoInsertAsync(string executableSql, Dict executableParams, bool ignoreIfDuplicate)
		{
			InsertStatement statement = new InsertStatement(this.database, executableSql);
			bool hasAutoIncrement = statement.StatementFromRefs[0].table.AutoIncrementFieldName != null;
			string newExecutableSql = hasAutoIncrement ? $"{executableSql} RETURNING {statement.StatementFromRefs[0].table.AutoIncrementFieldName}" : executableSql;

			var result = await ExecuteCommandAsync<int>(async c =>
			{
				if (!hasAutoIncrement)
					return await c.ExecuteNonQueryAsync();

				var id = 0;
				using (var reader = await c.ExecuteReaderAsync())
				{
					while (reader.Read())
					{
						int.TryParse(reader[0].ToString(), out id);
					}
				}
				return id;
			}, newExecutableSql, executableParams);

			return () => result;
		}

		protected override async Task<int> DoUpdateAsync(string executableSql, Dict executableParams)
		{
			var result = await ExecuteCommandAsync<int>(async c =>
			{
				return await c.ExecuteNonQueryAsync();
			}, executableSql, executableParams);

			return result;
		}

		protected override async Task<int> DoDeleteAsync(string executableSql, Dict executableParams)
		{
			var result = await ExecuteCommandAsync<int>(async c =>
			{
				return await c.ExecuteNonQueryAsync();
			}, executableSql, executableParams);

			return result;
		}

		protected override async Task DoTruncateAsync(string tableName)
		{
			await ExecuteCommandAsync<int>(async c =>
			{
				return await c.ExecuteNonQueryAsync();
			}, $"TRUNCATE {tableName}");
		}
		
		private T ExecuteCommand<T>(Func<NpgsqlCommand, T> query, string executableSql, Dict executableParams = null)
		{
			try
			{
				using (var command = new NpgsqlCommand(executableSql, this.connection, this.transaction))
				{
					if (executableParams != null)
						foreach (var param in executableParams)
							command.Parameters.AddWithValue(param.Key, NpgsqlDbType.Unknown, param.Value);

					return query(command);
				}
			}
			catch (PostgresException ex)
			{
				if (ex.Message.StartsWith("Duplicate entry"))
					throw new DuplicateKeyDatabaseException(ex.Message);

				throw new DatabaseException(ex.Message);
			}
			catch (Exception e)
			{
				logger.Error(e, $"Error executing {executableSql}...");
				throw e;
			}
		}

		private async Task<T> ExecuteCommandAsync<T>(Func<NpgsqlCommand, Task<T>> query, string executableSql, Dict executableParams = null)
		{
			try
			{
				using (var command = new NpgsqlCommand(executableSql, this.connection, this.transaction))
				{
					if (executableParams != null)
						foreach (var param in executableParams)
							command.Parameters.AddWithValue(param.Key, NpgsqlDbType.Unknown, param.Value);

					return await query(command);
				}
			}
			catch (PostgresException ex)
			{
				if (ex.Message.StartsWith("Duplicate entry"))
					throw new DuplicateKeyDatabaseException(ex.Message);

				throw new DatabaseException(ex.Message);
			}
			catch (Exception e)
			{
				logger.Error(e, $"Error executing {executableSql}...");
				throw e;
			}
		}
	}
}
