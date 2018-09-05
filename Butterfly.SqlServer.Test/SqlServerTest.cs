using Butterfly.Core.Database;
using Butterfly.Core.Test;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using System.Threading.Tasks;

namespace Butterfly.SqlServer.Test
{
	[TestClass]
	public class SqlServerTest
	{
        /*
         * Requires executing...
         * CREATE LOGIN test WITH PASSWORD = 'test!123';
         * CREATE USER test FOR LOGIN test;
         * exec sp_addrolemember 'db_owner', 'test';
         * CREATE DATABASE Butterfly;
         */
        //const string connectionString = @"Server=localhost; Initial Catalog=Butterfly; User ID=test; Password=test!123";

        const string connectionString = @"Data Source=(localdb)\ProjectsV13; Initial Catalog=Butterfly";
		const string dbSql = "Butterfly.SqlServer.Test.db.sql";

		[TestMethod]
		public async Task TestDatabase()
		{
			IDatabase database = new SqlServerDatabase(connectionString, forceIsolationLevelSnapshot: true);
			await DatabaseUnitTest.TestDatabase(database, Assembly.GetExecutingAssembly(), dbSql);
		}

		[TestMethod]
		public async Task TestDynamic()
		{
			var database = new SqlServerDatabase(connectionString, forceIsolationLevelSnapshot: true);
			await DynamicUnitTest.TestDatabase(database, Assembly.GetExecutingAssembly(), dbSql);
		}
	}
}
