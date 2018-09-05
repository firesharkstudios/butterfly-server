/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using Butterfly.Core.Database;
using NLog;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Dict = System.Collections.Generic.Dictionary<string, object>;

namespace Butterfly.SqlServer
{
	/// <inheritdoc/>
	public class SqlServerDatabase : BaseDatabase
	{
		private static readonly Logger logger = LogManager.GetCurrentClassLogger();

		//private static Regex LIMIT_REGEX = new Regex(@"^(SELECT).+(LIMIT\s+(.+?))$", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        protected readonly bool forceIsolationLevelSnapshot;

        /// <summary>
        /// Creates an instance of IDatabase that works with a MS SQL Server database (or LocalDB in Visual Studio)
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="forceIsolationLevelSnapshot">Setting this to true prevents SELECTs that depend on unfinished transactions from hanging (requires executing ALTER DATABASE {database} SET ALLOW_SNAPSHOT_ISOLATION ON on the database)</param>
        public SqlServerDatabase(string connectionString, bool forceIsolationLevelSnapshot = false) : base(connectionString) {
            this.forceIsolationLevelSnapshot = forceIsolationLevelSnapshot;

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

		protected override async Task<Dict[]> DoSelectRowsAsync(string executableSql, Dict executableParams, int limit)
		{
			var sql = ConvertLimit(executableSql, limit);

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

						rows.Add(row);
					}
				}

				return rows.ToArray();
			}, sql, executableParams);

			return result;
		}

        private readonly static Regex TOP_REGEX = new Regex(@"^SELECT\s", RegexOptions.IgnoreCase);
		private string ConvertLimit(string sql, int limit) {
            return limit > 0 ? TOP_REGEX.Replace(sql, $"SELECT TOP {limit} ") : sql;
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
			var indexes = await GetTableIndexesAsync(tableName);
			return new Table(tableName, fieldDefs, indexes);
		}

		private async Task<TableFieldDef[]> GetFieldDefsAsync(string tableName)
		{
			var sql = $@"select c.name,
								st.name as 'column_type',
								c.max_length,
								c.is_nullable,
								c.is_identity
				from sys.columns c
						join sys.tables t on c.object_id = t.object_id
						join sys.types st on c.system_type_id = st.system_type_id
				where t.name = '{tableName}'";

			var result = await ExecuteCommandAsync<List<TableFieldDef>>(async c =>
			{
				var fields = new List<TableFieldDef>();

				using (var reader = await c.ExecuteReaderAsync())
				{
					while (await reader.ReadAsync())
					{
						var field = new TableFieldDef(
							reader["name"].ToString(),
							SqlTypeToType(reader["column_type"].ToString()),
							int.Parse(reader["max_length"].ToString()),
							bool.Parse(reader["is_nullable"].ToString()),
							bool.Parse(reader["is_identity"].ToString())
						);

						fields.Add(field);
					}
				}

				return fields;
			}, sql);

			return result.ToArray();
		}

		private async Task<TableIndex[]> GetTableIndexesAsync(string tableName)
		{
			var sql = $@"select i.name, c.name as 'column_name', i.is_unique, i.is_primary_key from sys.indexes i
					join sys.tables t on i.object_id = t.object_id
					join sys.index_columns ic on i.object_id = ic.object_id and i.index_id = ic.index_id
					join sys.columns c on ic.object_id = c.object_id and ic.column_id = c.column_id
					where t.name = '{tableName}'
					order by i.name";

			var result = await ExecuteCommandAsync<List<TableIndex>>(async c =>
			{
				var uniqueIndexes = new List<TableIndex>();
				var lastIndexName = string.Empty;
				var lastFieldNames = new List<string>();
				var lastIndexType = TableIndexType.Other;

				using (var reader = await c.ExecuteReaderAsync())
				{
					while (await reader.ReadAsync())
					{
						var indexName = reader["name"].ToString();
						var columnName = reader["column_name"].ToString();
						var isPrimaryKey = bool.Parse(reader["is_primary_key"].ToString());
						var isUnique = bool.Parse(reader["is_unique"].ToString());

						if (lastIndexName != indexName)
						{
							if (lastFieldNames.Count > 0)
								uniqueIndexes.Add(new TableIndex(lastIndexType, lastFieldNames.ToArray()));
							
							lastIndexType = isPrimaryKey ? TableIndexType.Primary :
															isUnique ? TableIndexType.Unique :
															TableIndexType.Other;

							lastIndexName = indexName;
							lastFieldNames.Clear();
						}

						lastFieldNames.Add(columnName);
					}

					if (lastFieldNames.Count > 0)
						uniqueIndexes.Add(new TableIndex(lastIndexType, lastFieldNames.ToArray()));
				}

				return uniqueIndexes;
			}, sql);

			return result.ToArray();
		}
		
		private T ExecuteCommand<T>(Func<DbCommand, T> query, string executableSql, Dict executableParams = null)
		{
			try
			{
				using (var connection = new SqlConnection(ConnectionString))
				{
					connection.Open();

                    var sql = this.forceIsolationLevelSnapshot ? $"SET Transaction Isolation Level Snapshot {executableSql}" : executableSql;
                    using (var command = new SqlCommand(sql, connection))
					{
						if (executableParams != null)
							foreach (var param in executableParams)
								command.Parameters.Add(new SqlParameter(param.Key, param.Value));

						return query(command);
					}
				}
			}
			catch (SqlException ex)
			{
				throw new DatabaseException(ex.Message);
			}
			catch (Exception e)
			{
				logger.Error(e, $"Error executing {executableSql}...");
				throw e;
			}
		}

		private async Task<T> ExecuteCommandAsync<T>(Func<DbCommand, Task<T>> query, string executableSql, Dict executableParams = null)
		{
			try
			{
				using (var connection = new SqlConnection(ConnectionString))
				{
					await connection.OpenAsync();

                    var sql = this.forceIsolationLevelSnapshot ? $"SET Transaction Isolation Level Snapshot {executableSql}" : executableSql;
                    using (var command = new SqlCommand(sql, connection))
					{
						if (executableParams != null)
							foreach (var param in executableParams)
								command.Parameters.Add(new SqlParameter(param.Key, param.Value));

						return await query(command);
					}
				}
			}
			catch (SqlException ex)
			{
				throw new DatabaseException(ex.Message);
			}
			catch (Exception e)
			{
				logger.Error(e, $"Error executing {executableSql}...");
				throw e;
			}
		}

		// https://stackoverflow.com/a/18219394/5954805
		private Type SqlTypeToType(string type)
		{
			string[] tokens = type.Split(new char[] { '(', ')' }, StringSplitOptions.RemoveEmptyEntries);
			string typeFamily = tokens[0].ToLowerInvariant();
			string size = tokens.Length > 1 ? tokens[1] : string.Empty;

			switch (typeFamily)
			{
				case "bigint":
					return typeof(long);
				case "binary":
					return size == "1" ? typeof(byte) : typeof(byte[]);
				case "bit":
					return typeof(bool);
				case "char":
					return size == "1" ? typeof(char) : typeof(string);
				case "datetime":
					return typeof(DateTime);
				case "datetime2":
					return typeof(DateTime);
				case "decimal":
					return typeof(decimal);
				case "float":
					return typeof(double);
				case "image":
					return typeof(byte[]);
				case "int":
					return typeof(int);
				case "money":
					return typeof(decimal);
				case "nchar":
					return size == "1" ? typeof(char) : typeof(string);
				case "ntext":
					return typeof(string);
				case "nvarchar":
					return typeof(string);
				case "real":
					return typeof(float);
				case "uniqueidentifier":
					return typeof(Guid);
				case "smalldatetime":
					return typeof(DateTime);
				case "smallint":
					return typeof(short);
				case "smallmoney":
					return typeof(decimal);
				case "sql_variant":
					return typeof(object);
				case "text":
					return typeof(string);
				case "time":
					return typeof(TimeSpan);
				case "tinyint":
					return typeof(byte);
				case "varbinary":
					return typeof(byte[]);
				case "varchar":
					return typeof(string);
				case "variant":
					return typeof(string);
				case "xml":
					return typeof(string);
				default:
					throw new ArgumentException(string.Format("There is no .Net type specified for mapping T-SQL type '{0}'.", type));
			}
		}
	}
}
