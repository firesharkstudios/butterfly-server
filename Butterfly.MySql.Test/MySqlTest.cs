/* Any copyright is dedicated to the Public Domain.
 * http://creativecommons.org/publicdomain/zero/1.0/ */

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Butterfly.Core.Database;
using Butterfly.Core.Test;

namespace Butterfly.MySql.Test {
    [TestClass]
    public class MySqlDatabaseUnitTest {
        [TestMethod]
        public async Task DataMySqlDatabase() {
            IDatabase database = new Butterfly.MySql.MySqlDatabase("Server=127.0.0.1;Uid=test;Pwd=test!123;Database=butterfly_test");
            await DatabaseUnitTest.TestDatabase(database);
        }

        [TestMethod]
        public async Task DynamicMySqlDatabase() {
            BaseDatabase database = new Butterfly.MySql.MySqlDatabase("Server=127.0.0.1;Uid=test;Pwd=test!123;Database=butterfly_test");
            await DynamicUnitTest.TestDatabase(database);
        }
    }

}
