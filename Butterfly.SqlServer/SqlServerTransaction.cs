using Butterfly.Core.Database;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Threading.Tasks;

using Dict = System.Collections.Generic.Dictionary<string, object>;

namespace Butterfly.SqlServer
{
	public class SqlServerTransaction : BaseTransaction
	{
		protected SqlTransaction transaction;
		private SqlServerDatabase sqlServerDatabase;
		private SqlConnection sqlConnection;

		public SqlServerTransaction(SqlServerDatabase sqlServerDatabase) : base(sqlServerDatabase)
		{
			this.sqlServerDatabase = sqlServerDatabase;
		}

		public override void Begin()
		{
			sqlConnection = new SqlConnection(sqlServerDatabase.ConnectionString);
			sqlConnection.Open();
			transaction = sqlConnection.BeginTransaction();
		}

		public override async Task BeginAsync()
		{
			sqlConnection = new SqlConnection(sqlServerDatabase.ConnectionString);
		 	await sqlConnection.OpenAsync();
			transaction = sqlConnection.BeginTransaction();
		}

		public override void Dispose()
		{
			transaction.Dispose();
			sqlConnection.Dispose();
		}

		protected override void DoCommit()
		{
			transaction.Commit();
		}

		protected override async Task DoCommitAsync()
		{
			// no async commit available
			transaction.Commit();
		}

		protected override void DoRollback()
		{
			transaction.Rollback();
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

		protected override async Task<int> DoDeleteAsync(string executableSql, Dict executableParams)
		{
			var result = await ExecuteCommandAsync<int>(async c =>
			{
				return await c.ExecuteNonQueryAsync();
			}, executableSql);

			return result;
		}

		protected override async Task<Func<object>> DoInsertAsync(string executableSql, Dict executableParams, bool ignoreIfDuplicate)
		{
			executableSql += " SELECT SCOPE_IDENTITY();";

			var result = await ExecuteCommandAsync<object>(async c =>
			{
				return await c.ExecuteScalarAsync();
			}, executableSql, executableParams);

			return () => result;
		}

		protected override async Task DoTruncateAsync(string tableName)
		{
			await ExecuteCommandAsync<int>(async c =>
			{
				return await c.ExecuteNonQueryAsync();
			}, $"TRUNCATE TABLE {tableName}");
		}

		protected override async Task<int> DoUpdateAsync(string executableSql, Dictionary<string, object> executableParams)
		{
			var result = await ExecuteCommandAsync<int>(async c =>
			{
				return await c.ExecuteNonQueryAsync();
			}, executableSql);

			return result;
		}

		private T ExecuteCommand<T>(Func<DbCommand, T> query, string executableSql, Dict executableParams = null)
		{
			try
			{
				using (var command = new SqlCommand(executableSql, this.sqlConnection, this.transaction))
				{
					if (executableParams != null)
						foreach (var param in executableParams)
							command.Parameters.Add(new SqlParameter(param.Key, param.Value));

					return query(command);
				}
			}
			catch (SqlException ex)
			{
				throw new DatabaseException(ex.Message);
			}
			catch (Exception e)
			{
				throw e;
			}
		}

		private async Task<T> ExecuteCommandAsync<T>(Func<DbCommand, Task<T>> query, string executableSql, Dict executableParams = null)
		{
			try
			{
				using (var command = new SqlCommand(executableSql, this.sqlConnection, this.transaction))
				{
					if (executableParams != null)
						foreach (var param in executableParams)
							command.Parameters.Add(new SqlParameter(param.Key, param.Value));

					return await query(command);
				}
			}
			catch (SqlException ex)
			{
				throw new DatabaseException(ex.Message);
			}
			catch (Exception e)
			{
				throw e;
			}
		}

	}
}