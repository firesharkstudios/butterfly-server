using Butterfly.Core.Test;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using System.Threading.Tasks;

namespace Butterfly.Postgres.Test
{
	[TestClass]
	public class PostgresTest
	{
        // CREATE USER test WITH PASSWORD 'test!123';
        const string connectionString = "User ID=test;Password=test!123;Host=localhost;Port=5432;Database=test;";
		const string dbSql = "Butterfly.Postgres.Test.db.sql";

		[TestMethod]
		public async Task TestDatabase()
		{
			var database = new PostgresDatabase(connectionString);
			await DatabaseUnitTest.TestDatabase(database, Assembly.GetExecutingAssembly(), dbSql);
		}

		[TestMethod]
		public async Task TestDynamic()
		{
			var database = new PostgresDatabase(connectionString);
			await DynamicUnitTest.TestDatabase(database, Assembly.GetExecutingAssembly(), dbSql);
		}
	}
}
