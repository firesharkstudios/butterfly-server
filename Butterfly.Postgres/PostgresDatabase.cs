/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Data.Common;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Npgsql;
using NLog;

using Butterfly.Core.Database;

using Dict = System.Collections.Generic.Dictionary<string, object>;
using NpgsqlTypes;

namespace Butterfly.Postgres
{
		/// <inheritdoc/>
		public class PostgresDatabase : BaseDatabase
		{

				private static readonly Logger logger = LogManager.GetCurrentClassLogger();

				public PostgresDatabase(string connectionString) : base(connectionString)
				{
				}

				public override bool CanJoin => true;

				public override bool CanFieldAlias => true;

				protected override async Task LoadSchemaAsync()
				{
						string commandText = "SELECT table_name FROM information_schema.tables WHERE table_schema='public'";

						var result = await ExecuteCommandAsync<int>(async c =>
						{
								using (var reader = await c.ExecuteReaderAsync())
								{
										while (reader.Read())
										{
												string tableName = reader[0].ToString();
												Table table = await this.LoadTableSchemaAsync(tableName);
												this.tableByName[table.Name] = table;
										}
								}

								return tableByName.Count;
						}, commandText);
				}

				protected override async Task<Table> LoadTableSchemaAsync(string tableName)
				{
						TableFieldDef[] fieldDefs = await this.GetFieldDefsAsync(tableName);
						TableIndex[] uniqueIndexes = await this.GetUniqueIndexesAsync(tableName);
						return new Table(tableName, fieldDefs, uniqueIndexes);
				}

				private async Task<TableFieldDef[]> GetFieldDefsAsync(string tableName)
				{
						string commandText = $@"select column_name, data_type, character_maximum_length, is_nullable, column_default, is_identity 
						                        from INFORMATION_SCHEMA.COLUMNS where table_name = @tableName";
						var parms = new Dict
						{
								{ "tableName", tableName }
						};

						var result = await ExecuteCommandAsync<List<TableFieldDef>>(async c =>
						{
								var fields = new List<TableFieldDef>();

								using (var reader = await c.ExecuteReaderAsync())
								{
										while (reader.Read())
										{
												var name = reader["column_name"].ToString();
												var typeText = reader["data_type"].ToString();
												var maxLengthText = reader["character_maximum_length"].ToString();
												var allowNullText = reader["is_nullable"].ToString();
												var defaultText = reader["column_default"].ToString();
												var isIdentity = reader["is_identity"].ToString();

												if (!int.TryParse(maxLengthText, out int maxLength)) maxLength = -1;
												bool allowNull = allowNullText.Equals("YES", StringComparison.OrdinalIgnoreCase);
												bool isAutoIncrement = isIdentity.Equals("YES", StringComparison.OrdinalIgnoreCase);
												(Type type, _) = ConvertType(typeText);
												fields.Add(new TableFieldDef(name, type, maxLength, allowNull, isAutoIncrement));
										}
								}

								return fields;
						}, commandText, parms);
						
						return result.ToArray();
				}

				private async Task<TableIndex[]> GetUniqueIndexesAsync(string tableName)
				{
						string commandText = @"SELECT
                  trel.relname AS table_name,
                  irel.relname AS index_name,
                  i.indisunique AS is_unique,
                  i.indisprimary AS is_primary,  
                  array_agg (a.attname ORDER BY c.ordinality) AS columns
                FROM pg_index AS i
                JOIN pg_class AS trel ON trel.oid = i.indrelid
                JOIN pg_namespace AS tnsp ON trel.relnamespace = tnsp.oid
                JOIN pg_class AS irel ON irel.oid = i.indexrelid
                CROSS JOIN LATERAL unnest (i.indkey) WITH ORDINALITY AS c (colnum, ordinality)
                JOIN pg_attribute AS a ON trel.oid = a.attrelid AND a.attnum = c.colnum
                WHERE trel.relname=@tableName AND i.indisunique='True'
                GROUP BY tnsp.nspname, trel.relname, irel.relname, i.indisunique, i.indisprimary";

						var parms = new Dict
						{
								{ "tableName", tableName }
						};

						var result = await ExecuteCommandAsync<List<TableIndex>>(async c =>
						{
								var uniqueIndexes = new List<TableIndex>();

								using (var reader = await c.ExecuteReaderAsync())
								{
										while (reader.Read())
										{
												var indexName = reader["index_name"].ToString();
												var isUnique = bool.Parse(reader["is_unique"].ToString());
												var isPrimaryKey = bool.Parse(reader["is_primary"].ToString());
												var fieldNames = (string[])reader["columns"];

												var tableIndexType = isPrimaryKey ? TableIndexType.Primary :
															isUnique ? TableIndexType.Unique :
															TableIndexType.Other;
												
												uniqueIndexes.Add(new TableIndex(tableIndexType, fieldNames));
										}
								}

								return uniqueIndexes;
						}, commandText, parms);

						return result.ToArray();
				}

				protected override BaseTransaction CreateTransaction()
				{
						return new PostgresTransaction(this);
				}

				protected override async Task<Dict[]> DoSelectRowsAsync(string executableSql, Dict executableParams, int limit)
				{
						var sql = limit > 0 ? $"{executableSql} LIMIT {limit}" : executableSql;
						var result = await ExecuteCommandAsync<Dict[]>(async c =>
						{
								var rows = new List<Dict>();
								using (var reader = await c.ExecuteReaderAsync())
								{
										ReadOnlyCollection<DbColumn> columns = null;
										while (await reader.ReadAsync())
										{
												if (columns == null) columns = reader.GetColumnSchema();
												var row = new Dict();
												
												foreach (var column in columns)
												{
														row[column.ColumnName] = ConvertValue(reader[column.ColumnName]);
												}
												rows.Add(row);
										}
								}

								return rows.ToArray();
						}, sql, executableParams);
						

						return result;
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

				protected static object ConvertValue(object value)
				{
						if (value == null || value == DBNull.Value)
						{
								return null;
						}
						else
						{
								return value;
						}
				}

				public static (Type, int) ConvertType(string text)
				{
						Match match = PARSE_TYPE.Match(text);
						if (!match.Success) throw new Exception($"Could not parse SQL type '{text}'");

						string typeText = match.Groups["type"].Value;

						Type type;
						if (typeText.StartsWith("CHARACTER", StringComparison.OrdinalIgnoreCase))
						{
								type = typeof(string);
						}
						else if (typeText.Equals("INTEGER", StringComparison.OrdinalIgnoreCase))
						{
								type = typeof(long);
						}
						else if (typeText.Equals("BIGINT", StringComparison.OrdinalIgnoreCase))
						{
								type = typeof(long);
						}
						else if (typeText.Equals("REAL", StringComparison.OrdinalIgnoreCase))
						{
								type = typeof(float);
						}
						else if (typeText.Equals("DOUBLE PRECISION", StringComparison.OrdinalIgnoreCase))
						{
								type = typeof(double);
						}
						else if (typeText.StartsWith("TIMESTAMP", StringComparison.OrdinalIgnoreCase))
						{
								type = typeof(DateTime);
						}
						else
						{
								throw new Exception($"Unknown field type '{text}'");
						}

						string maxLengthText = match.Groups["maxLengthWithParens"].Value.Replace("(", "").Replace(")", "");
						if (!int.TryParse(maxLengthText, out int maxLength)) maxLength = -1;

						return (type, maxLength);
				}


				private T ExecuteCommand<T>(Func<NpgsqlCommand, T> query, string executableSql, Dict executableParams = null)
				{
						try
						{
								using (var connection = new NpgsqlConnection(this.ConnectionString))
								using (var command = new NpgsqlCommand(executableSql, connection))
								{
										connection.Open();

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
								using (var connection = new NpgsqlConnection(this.ConnectionString))
								using (var command = new NpgsqlCommand(executableSql, connection))
								{
										await connection.OpenAsync();

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
