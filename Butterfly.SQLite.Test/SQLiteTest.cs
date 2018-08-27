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
        public async Task DataMySqlDatabase() {
            string fileName = Path.GetTempFileName();
            IDatabase database = new Butterfly.SQLite.SQLiteDatabase(fileName);
            await DatabaseUnitTest.TestDatabase(database);
        }

        [TestMethod]
        public async Task DynamicMySqlDatabase() {
            string fileName = Path.GetTempFileName();
            IDatabase database = new Butterfly.SQLite.SQLiteDatabase(fileName);
            await DynamicUnitTest.TestDatabase(database);
        }
    }

}
