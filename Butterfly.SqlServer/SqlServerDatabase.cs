using Butterfly.Core.Database;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Text;
using System.Threading.Tasks;

using Dict = System.Collections.Generic.Dictionary<string, object>;

namespace Butterfly.SqlServer
{
	public class SqlServerDatabase : BaseDatabase
	{
		internal SqlConnection sqlConnection;

		public SqlServerDatabase(string connectionString) : base(connectionString) {
		}

		internal void Begin()
		{
			sqlConnection = new SqlConnection(this.ConnectionString);
			sqlConnection.Open();
		}

		internal async Task BeginAsync()
		{
			sqlConnection = new SqlConnection(this.ConnectionString);
			await sqlConnection.OpenAsync();
		}

		public override bool CanJoin => true;

		public override bool CanFieldAlias => true;

		protected override BaseTransaction CreateTransaction()
		{
			return new SqlServerTransaction(this);
		}

		protected override async Task<Dict[]> DoQueryRowsAsync(string storedProcedureName, Dict executableParams)
		{
			var result = await ExecuteCommandAsync<Dict[]>(async c =>
			{
				var rows = new List<Dict>();
				using (var reader = await c.ExecuteReaderAsync())
				{
					while (await reader.ReadAsync())
					{
						var row = new Dict();
						for (int i = 0; i < reader.FieldCount; i++)
						{
							row[reader.GetName(i)] = reader[i];
						}
					}
				}

				return rows.ToArray();
			}, storedProcedureName, executableParams);

			return result;
		}

		protected override async Task<Dict[]> DoSelectRowsAsync(string executableSql, Dict executableParams)
		{
			var result = await ExecuteCommandAsync<Dict[]>(async c =>
			{
				var rows = new List<Dict>();
				using (var reader = await c.ExecuteReaderAsync())
				{
					var columns = reader.GetColumnSchema();
					while (await reader.ReadAsync())
					{
						var row = new Dict();
						foreach (var column in columns)
						{
							row[column.ColumnName] = reader[column.ColumnName];
						}
					}
				}

				return rows.ToArray();
			}, executableSql, executableParams);

			return result;
		}

		protected override async Task LoadSchemaAsync()
		{
			const string sql = "select * from sys.tables";
			var result = await ExecuteCommandAsync<int>(async c =>
			{
				using (var reader = await c.ExecuteReaderAsync())
				{
					while (await reader.ReadAsync())
					{
						var tableName = reader[0].ToString();
						var table = await LoadTableSchemaAsync(tableName);
						tableByName[tableName] = table;
					}
				}

				return tableByName.Count;
			}, sql);
		}

		protected override async Task<Table> LoadTableSchemaAsync(string tableName)
		{
			var fieldDefs = await GetFieldDefsAsync(tableName);
			TableIndex[] indexes = null; // todo - get indexes
			return new Table(tableName, fieldDefs, indexes);
		}

		private async Task<TableFieldDef[]> GetFieldDefsAsync(string tableName)
		{
			var sql = $@"select c.name,
								st.name as 'column_type',
								c.is_nullable,
								c.is_identity
				from sys.columns c
						join sys.tables t on c.object_id = t.object_id
						join sys.types st on c.system_type_id = st.system_type_id
				where t.name = '{tableName}'";

			var result = await ExecuteCommandAsync<List<TableFieldDef>>(async c =>
			{
				List<TableFieldDef> fields = new List<TableFieldDef>();

				using (var reader = await c.ExecuteReaderAsync())
				{
					while (await reader.ReadAsync())
					{
						var field = new TableFieldDef(
							reader["name"].ToString(),
							Type.GetType(reader["column_type"].ToString()), // todo - convert type
							int.Parse(reader["max_length"].ToString()),
							reader["is_nullable"].ToString() == "1",
							reader["is_identity"].ToString() == "1"
						);

						fields.Add(field);
					}
				}

				return fields;
			}, sql);

			return result.ToArray();
		}
		
		internal T ExecuteCommand<T>(Func<DbCommand, T> query, string executableSql, Dict executableParams = null)
		{
			try
			{
				using (var command = new SqlCommand(executableSql, sqlConnection))
				{
					return query(command);
				}
			}
			catch (SqlException ex)
			{
				throw new DatabaseException(ex.Message);
			}
		}

		internal async Task<T> ExecuteCommandAsync<T>(Func<DbCommand, Task<T>> query, string executableSql, Dict executableParams = null)
		{
			try
			{
				using (var command = new SqlCommand(executableSql, sqlConnection))
				{
					return await query(command);
				}
			}
			catch (SqlException ex)
			{
				throw new DatabaseException(ex.Message);
			}
		}
		
	}
}
