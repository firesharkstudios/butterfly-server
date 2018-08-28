using Butterfly.Core.Database;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Butterfly.SqlServer
{
	public class SqlServerTransaction : BaseTransaction
	{
		public SqlServerTransaction(SqlServerDatabase sqlServerDatabase) : base(sqlServerDatabase)
		{
		}

		public override void Begin()
		{
			throw new NotImplementedException();
		}

		public override Task BeginAsync()
		{
			throw new NotImplementedException();
		}

		public override void Dispose()
		{
			throw new NotImplementedException();
		}

		protected override void DoCommit()
		{
			throw new NotImplementedException();
		}

		protected override Task DoCommitAsync()
		{
			throw new NotImplementedException();
		}

		protected override bool DoCreate(CreateStatement statement)
		{
			throw new NotImplementedException();
		}

		protected override Task<bool> DoCreateAsync(CreateStatement statement)
		{
			throw new NotImplementedException();
		}

		protected override Task<int> DoDeleteAsync(string executableSql, Dictionary<string, object> executableParams)
		{
			throw new NotImplementedException();
		}

		protected override Task<Func<object>> DoInsertAsync(string executableSql, Dictionary<string, object> executableParams, bool ignoreIfDuplicate)
		{
			throw new NotImplementedException();
		}

		protected override void DoRollback()
		{
			throw new NotImplementedException();
		}

		protected override Task DoTruncateAsync(string tableName)
		{
			throw new NotImplementedException();
		}

		protected override Task<int> DoUpdateAsync(string executableSql, Dictionary<string, object> executableParams)
		{
			throw new NotImplementedException();
		}
	}
}