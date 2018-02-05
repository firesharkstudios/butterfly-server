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

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

using Butterfly.Database.Dynamic;
using Butterfly.Database.Event;

using Dict = System.Collections.Generic.Dictionary<string, object>;

namespace Butterfly.Database {
    /// <summary>
    /// Allows executing SELECT statements, creating transactions to execute INSERT, UPDATE, and DELETE statements; 
    /// creating dynamic views; and receiving data change events both on tables and dynamic views.
    /// </summary>
    /// <remarks>
    /// Adding records and echoing all data change events to the console...<para/>
    /// <code>
    ///     // Create database instance (will also read the schema from the database)
    ///     var database = new SomeDatabase();
    ///     
    ///     // Listen for all database data events
    ///     var databaseListener = database.OnNewCommittedTransaction(dataEventTransaction => {
    ///         console.WriteLine($"Low Level DataEventTransaction={dataEventTransaction}");
    ///     });
    ///     
    ///     // INSERT a couple of records (this will cause a single data even transaction with
    ///     // two INSERT data events to be written to the console above)
    ///     using (var transaction = database.BeginTransaction()) {
    ///         await database.InsertAndCommitAsync("employee", values: new {
    ///             department_id: 1,
    ///             name: "SpongeBob"
    ///         });
    ///         await database.InsertAndCommitAsync("employee", values: new {
    ///             department_id: 1,
    ///             name: "Squidward"
    ///         });
    ///         await database.CommitAsync();
    ///     );
    /// </code>
    /// 
    /// Creating a DynamicView and echoing data change events on the DynamicView to the console...<para/>
    /// <code>
    ///     // Create database instance (will also read the schema from the database)
    ///     var database = new SomeDatabase();
    ///     
    ///     // Create a DynamicViewSet that print any data events to the console
    ///     // (this will immediately echo an INITIAL data event for each existing matching record)
    ///     var dynamicViewSet = database.CreateAndStartDynamicViewSet(
    ///         "SELECT * FROM employee WHERE department_id=@departmentId", 
    ///         new {
    ///             departmentId = 1
    ///         },
    ///         dataEventTransaction => {
    ///             Console.WriteLine(dataEventTransaction);
    ///         }
    ///     );
    /// 
    ///     // This will cause the above DynamicViewSet to echo an INSERT data event
    ///     await database.InsertAndCommitAsync("employee", values: new {
    ///         department_id: 1
    ///         name: "Mr Crabs"
    ///     });
    ///     
    ///     // This will NOT cause the above DynamicViewSet to echo an INSERT data event
    ///     // (because the department_id doesn't match)
    ///     await database.InsertAndCommitAsync("employee", values: new {
    ///         department_id: 2
    ///         name: "Patrick Star"
    ///     });
    /// </code>
    /// </remarks>
    public interface IDatabase {

        bool CanJoin { get;  }

        /// <summary>
        /// Dictionary of <see cref="Table"/> instances keyed by name
        /// </summary>
        Dictionary<string, Table> Tables { get; }

        /// <summary>
        /// Creates database tables from an embedded resource file by internally calling <see cref="CreateFromText(string)"/> with the contents of the embedded resource file"
        /// </summary>
        /// <param name="assembly"></param>
        /// <param name="resourceFile"></param>
        /// <returns></returns>
        void CreateFromResourceFile(Assembly assembly, string resourceFile);

        /// <inheritDoc cref="CreateFromResourceFile(Assembly, string)"/>
        Task CreateFromResourceFileAsync(Assembly assembly, string resourceFile);

        /// <summary>
        /// Creates database tables from a string containing a semicolon delimited series of CREATE statements in MySQL format (will be converted to native database format as appropriate).<para/>
        /// Lines beginning with <code>--</code> will be ignored. Each CREATE statement must include a PRIMARY KEY definition. If the table already exists, the CREATE statement is ignored.<para/>
        /// Creating database tables with this method is not required (primarily done as a convenience method for unit testing)"/>.
        /// </summary>
        /// <param name="createStatements"></param>
        void CreateFromText(string createStatements);

        /// <inheritDoc cref="CreateFromText(string)"/>
        Task CreateFromTextAsync(string createStatements);

        /// <summary>
        /// Adds a listener that is invoked when there is a new uncommitted transaction
        /// </summary>
        /// <param name="listener">The lambda to call when there is a new uncommitted <see cref="DataEventTransaction"/></param>
        /// <returns>An <see cref="IDisposable"/> that allows removing the listener by calling Dispose()</returns>
        IDisposable OnNewUncommittedTransaction(Action<DataEventTransaction> listener);

        /// <inheritdoc cref="OnNewUncommittedTransaction(Action{DataEventTransaction})"/>
        IDisposable OnNewUncommittedTransaction(Func<DataEventTransaction, Task> listener);

        /// <summary>
        /// Adds a listener that is invoked when there is a new committed transaction
        /// </summary>
        /// <param name="listener">The lambda to call when there is a new committed <see cref="DataEventTransaction"/></param>
        /// <returns>An <see cref="IDisposable"/> that allows removing the listener by calling Dispose()</returns>
        IDisposable OnNewCommittedTransaction(Action<DataEventTransaction> listener);

        /// <inheritdoc cref="OnNewCommittedTransaction(Action{DataEventTransaction})"/>
        IDisposable OnNewCommittedTransaction(Func<DataEventTransaction, Task> listener);

        /// <summary>
        /// Executes the SELECT statement and return the value of the first column of the first row (the SELECT statement may contain vars like @name specified in <paramref name="vars"/>).<para/>
        /// <para/>
        /// If a var is null then references in the WHERE clause like <code>name=@name</code> will be rewritten as <code>name IS NULL</code> and references in the WHERE clause like <code>name!=@name</code> will be rewritten as <code>name IS NOT NULL</code>.<para/>
        /// <para/>
        /// If a var is an array then references in the WHERE clause like <code>name=@name</code> will be rewritten as <code>1=2</code> when the array is empty, rewritten as <code>name='Bob'</code> when the array contains a single element 'Bob', and rewritten as <code>name IN ('Bob', 'Jim')</code> when the array contains elements 'Bob' and 'Jim'.<para/>
        /// </summary>
        /// <typeparam name="T">The return type of the single value returned</typeparam>
        /// <param name="selectStatement">The SELECT statement to execute (may contain vars like @name specified in <paramref name="vars"/>)</param>
        /// <param name="vars">Either an anonymous type or Dictionary with the vars used in the SELECT statement</param>
        /// <param name="defaultValue">The value to return if no rows were returned or the value of the first column of the first row is null</param>
        /// <returns>The value of the first column of the first row</returns>
        Task<T> SelectValueAsync<T>(string selectStatement, dynamic vars = null, T defaultValue = default(T));

        /// <summary>
        /// Executes the SELECT statement and return the first row (the SELECT statement may contain vars like @name specified in <paramref name="vars"/>)
        /// </summary>
        /// <param name="selectStatement">The SELECT statement to execute (may contain vars like @name specified in <paramref name="vars"/>)</param>
        /// <param name="vars"></param>
        /// <returns></returns>
        Task<Dict> SelectRowAsync(string selectStatement, dynamic vars = null);

        /// <summary>
        /// Executes the SELECT statement and return the rows (the SELECT statement may contain vars like @name specified in <paramref name="vars"/>)
        /// </summary>
        /// <param name="selectStatement"></param>
        /// <param name="vars"></param>
        /// <returns></returns>
        Task<Dict[]> SelectRowsAsync(string selectStatement, dynamic vars = null);

        /// <summary>
        /// Executes an INSERT statement as a single transaction
        /// </summary>
        /// <remarks>
        /// Do an INSERT using the table name and an anonymous type...
        /// <code>
        /// await database.InsertAndCommitAsync("message", new {
        ///     text = "Hello",
        ///     owner_id = "123",
        /// });
        /// </code>
        /// Do an INSERT using a full statement and a Dictionary...
        /// <code>
        /// await database.InsertAndCommitAsync("INSERT INTO message (text, owner_id) VALUES (@t, @oid)", new Dictionary&lt;string, object&gt; {
        ///     ["t"] = "Hello",
        ///     ["oid"] = "123",
        /// });
        /// </code>
        /// </remarks>
        /// <param name="insertStatement">
        ///     Either a table name or a full INSERT statement with vars prefixed by @ (like <code>@name</code>)
        /// </param>
        /// <param name="vars">
        ///     Either an anonymous type or a Dictionary. 
        ///     If <paramref name="insertStatement"/> is a table name, the <paramref name="vars"/> values will be used to build the UPDATE statement.
        ///     If <paramref name="insertStatement"/> is a full INSERT statement, there must be one entry for each var referenced in <paramref name="insertStatement"/>.
        /// </param>
        /// <param name="ignoreIfDuplicate">
        ///     If the INSERT attempts to duplicate the primary key then either 
        ///     throw an <see cref="DuplicateKeyDatabaseException"/> error if <paramref name="ignoreIfDuplicate"/> is true
        ///     or just ignore if <paramref name="ignoreIfDuplicate"/> is false
        /// </param>
        /// <returns>Primary key value (semi-colon delimited string if multi-field primary key)</returns>
        Task<T> InsertAndCommitAsync<T>(string insertStatement, dynamic vars, bool ignoreIfDuplicate = false);

        /// <summary>
        /// Executes an UPDATE statement as a single transaction
        /// </summary>
        /// <remarks>
        /// Do an UPDATE using the table name and an anonymous type...
        /// <code>
        /// await database.UpdateAndCommitAsync("message", new {
        ///     id = 123,
        ///     text = "Hello",
        /// });
        /// </code>
        /// Do an UPDATE using a full statement and a Dictionary...
        /// <code>
        /// await database.UpdateAndCommitAsync("UPDATE message SET text=@t WHERE id=@id", new Dictionary&lt;string, object&gt; {
        ///     ["id"] = 123,
        ///     ["t"] = "Hello",
        /// });
        /// </code>
        /// </remarks>
        /// <param name="updateStatement">
        ///     Either a table name or a full UPDATE statement with vars prefixed by @ (like <code>@name</code>)
        /// </param>
        /// <param name="vars">
        ///     Either an anonymous type or a Dictionary. 
        ///     If <paramref name="updateStatement"/> is a table name, the <paramref name="vars"/> values will be used to build the SET clause and WHERE clause of the UPDATE statement.
        ///     If <paramref name="updateStatement"/> is a full UPDATE statement, there must be one entry for each var referenced in <paramref name="updateStatement"/>.
        /// </param>
        /// <returns>Number of records updated</returns>
        Task<int> UpdateAndCommitAsync(string updateStatement, dynamic vars);

        /// <summary>
        /// Executes a DELETE statement as a single transaction
        /// </summary>
        /// <remarks>
        /// Do a DELETE using the table name and an anonymous type...
        /// <code>
        /// await database.DeleteAndCommitAsync("message", new {
        ///     id = 123
        /// });
        /// </code>
        /// Do a DELETE using a full statement and a Dictionary...
        /// <code>
        /// await database.DeleteAndCommitAsync("DELETE FROM message WHERE id=@id", new Dictionary&lt;string, object&gt; {
        ///     ["id"] = 123
        /// });
        /// </code>
        /// </remarks>
        /// <param name="deleteStatement">
        ///     Either a table name or a full DELETE statement with vars prefixed by @ (like <code>@name</code>)
        /// </param>
        /// <param name="vars">
        ///     Either an anonymous type or a Dictionary. 
        ///     If <paramref name="deleteStatement"/> is a table name, the <paramref name="vars"/> values will be used to build the WHERE clause of the DELETE statement.
        ///     If <paramref name="deleteStatement"/> is a full DELETE statement, there must be one entry for each var referenced in <paramref name="deleteStatement"/>.
        /// </param>
        /// <returns>Number of records deleted</returns>
        Task<int> DeleteAndCommitAsync(string deleteStatement, dynamic vars);

        /// <summary>
        /// Creates a new <see cref="ITransaction"/> instance.  An <see cref="ITransaction"/> instance allows performing an atomic set of modifications to the database.  Must execute <see cref="ITransaction.CommitAsync"/> to save the transaction changes.  Disposing the transaction without committing rolls back the changes.
        /// </summary>
        /// <returns>An <see cref="ITransaction"/> instance (can then call InsertAsync(), UpdateAsync(), or DeleteAsync() on the ITransaction instance to make changes on the transaction)/></returns>
        ITransaction BeginTransaction();

        /// <inheritdoc cref="BeginTransaction"/>
        Task<ITransaction> BeginTransactionAsync();

        /// <summary>
        /// Allows specifying a lambda that creates a default value for a field when executing an INSERT.  If <paramref name="tableName"/> is null, the <paramref name="getDefaultValue"/> lambda will be applied to all tables.
        /// </summary>
        /// <param name="fieldName">Name of the field</param>
        /// <param name="getDefaultValue">The lambda that returns the default value</param>
        /// <param name="tableName">An optional table name.  If not null, the getDefaultValue lambda is only applied to the specified table. If null, the getDefaultValue lambda is applied to all tables.</param>
        void SetInsertDefaultValue(string fieldName, Func<string, object> getDefaultValue, string tableName = null);

        /// <summary>
        /// Allows creating a set of <see cref="DynamicView"/> instances that publish a single <see cref="DataEventTransaction"/> instance with initial data and new <see cref="DataEventTransaction"/> instances when data changes.  The <see cref="DataEventTransaction"/> instances are published to the lambda passed as the <paramref name="listener"/>.
        /// </summary>
        /// <param name="listener">The lambda that should receive the <see cref="DataEventTransaction"/> instances</param>
        /// <returns>A <see cref="DynamicViewSet"/> that can be disposed to remove all database listners</returns>
        DynamicViewSet CreateDynamicViewSet(Action<DataEventTransaction> listener);

        /// <inheritdoc cref="CreateDynamicViewSet(Action{DataEventTransaction})"/>
        DynamicViewSet CreateDynamicViewSet(Func<DataEventTransaction, Task> listener);

        /// <summary>
        /// Convenience method which creates a <see cref="DynamicViewSet"/>, adds a single <see cref="DynamicView"/> to the <see cref="DynamicViewSet"/>, and starts the <see cref="DynamicViewSet"/>.
        /// </summary>
        /// <param name="selectStatement"></param>
        /// <param name="listener"></param>
        /// <param name="values"></param>
        /// <param name="name"></param>
        /// <param name="keyFieldNames"></param>
        /// <returns></returns>
        Task<DynamicViewSet> CreateAndStartDynamicView(string selectStatement, Action<DataEventTransaction> listener, dynamic values = null, string name = null, string[] keyFieldNames = null);

        /// <inheritdoc cref="CreateAndStartDynamicView(string, Action{DataEventTransaction}, dynamic, string, string[])"/>
        Task<DynamicViewSet> CreateAndStartDynamicView(string selectStatement, Func<DataEventTransaction, Task> listener, dynamic values = null, string name = null, string[] keyFieldNames = null);
    }
}
