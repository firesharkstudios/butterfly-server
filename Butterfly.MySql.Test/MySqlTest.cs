/*
 * Copyright 2017 Fireshark Studios, LLC
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

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
