/* Any copyright is dedicated to the Public Domain.
 * http://creativecommons.org/publicdomain/zero/1.0/ */

using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Butterfly.Core.Database;
using Butterfly.Core.Test;

namespace Butterfly.MySql.Test {
    [TestClass]
    public class SQLiteTest {
        [TestMethod]
        public async Task TestDatabase() {
            // See https://github.com/aspnet/Microsoft.Data.Sqlite/wiki/Connection-Strings
            IDatabase database = new Butterfly.SQLite.SQLiteDatabase("Filename=./my_database.db");
            await DatabaseUnitTest.TestDatabase(database);
        }

        [TestMethod]
        public async Task TestDynamic() {
            // See https://github.com/aspnet/Microsoft.Data.Sqlite/wiki/Connection-Strings
            IDatabase database = new Butterfly.SQLite.SQLiteDatabase("Filename=./my_database.db");
            await DynamicUnitTest.TestDatabase(database);
        }
    }

}
