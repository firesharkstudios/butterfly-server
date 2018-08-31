using Butterfly.Core.Database;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

using Dict = System.Collections.Generic.Dictionary<string, object>;

namespace Butterfly.SqlServer
{
	public class SqlServerTransaction : BaseTransaction
	{
		protected SqlTransaction transaction;
		private SqlServerDatabase sqlServerDatabase;

		public SqlServerTransaction(SqlServerDatabase sqlServerDatabase) : base(sqlServerDatabase)
		{
			this.sqlServerDatabase = sqlServerDatabase;
		}

		public override void Begin()
		{
			sqlServerDatabase.Begin();
		}

		public override async Task BeginAsync()
		{
			await sqlServerDatabase.BeginAsync();
			transaction = sqlServerDatabase.sqlConnection.BeginTransaction();
		}

		public override void Dispose()
		{
			sqlServerDatabase.sqlConnection.Dispose();
			transaction = sqlServerDatabase.sqlConnection.BeginTransaction();
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
			var result = sqlServerDatabase.ExecuteCommand<int>(c =>
			{
				return c.ExecuteNonQuery();
			}, statement.Sql, sqlTran: transaction);
			
			return false;
		}

		protected override async Task<bool> DoCreateAsync(CreateStatement statement)
		{
			var result = await sqlServerDatabase.ExecuteCommandAsync<int>(async c =>
			{
				return await c.ExecuteNonQueryAsync();
			}, statement.Sql, sqlTran: transaction);
			
			return false;
		}

		protected override async Task<int> DoDeleteAsync(string executableSql, Dict executableParams)
		{
			var result = await sqlServerDatabase.ExecuteCommandAsync<int>(async c =>
			{
				return await c.ExecuteNonQueryAsync();
			}, executableSql, sqlTran: transaction);

			return result;
		}

		protected override async Task<Func<object>> DoInsertAsync(string executableSql, Dict executableParams, bool ignoreIfDuplicate)
		{
			var result = await sqlServerDatabase.ExecuteCommandAsync<object>(async c =>
			{
				return await c.ExecuteScalarAsync();
			}, executableSql, executableParams, sqlTran: transaction);

			return () => result;
		}

		protected override async Task DoTruncateAsync(string tableName)
		{
			await sqlServerDatabase.ExecuteCommandAsync<int>(async c =>
			{
				return await c.ExecuteNonQueryAsync();
			}, $"TRUNCATE TABLE {tableName}", sqlTran: transaction);
		}

		protected override async Task<int> DoUpdateAsync(string executableSql, Dictionary<string, object> executableParams)
		{
			var result = await sqlServerDatabase.ExecuteCommandAsync<int>(async c =>
			{
				return await c.ExecuteNonQueryAsync();
			}, executableSql, sqlTran: transaction);

			return result;
		}
	}
}