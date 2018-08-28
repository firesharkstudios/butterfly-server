using Butterfly.Core.Database;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Butterfly.SqlServer
{
	public class SqlServerDatabase : BaseDatabase
	{
		public SqlServerDatabase(string connectionString) : base(connectionString) {
		}

		public override bool CanJoin => throw new NotImplementedException();

		public override bool CanFieldAlias => throw new NotImplementedException();

		protected override BaseTransaction CreateTransaction()
		{
			throw new NotImplementedException();
		}

		protected override Task<Dictionary<string, object>[]> DoQueryRowsAsync(string storedProcedureName, Dictionary<string, object> executableParams)
		{
			throw new NotImplementedException();
		}

		protected override Task<Dictionary<string, object>[]> DoSelectRowsAsync(string executableSql, Dictionary<string, object> executableParams)
		{
			throw new NotImplementedException();
		}

		protected override Task LoadSchemaAsync()
		{
			throw new NotImplementedException();
		}

		protected override Task<Table> LoadTableSchemaAsync(string tableName)
		{
			throw new NotImplementedException();
		}
	}
}
