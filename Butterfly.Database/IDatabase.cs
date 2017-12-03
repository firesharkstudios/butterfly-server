using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

using Butterfly.Database.Event;

using Dict = System.Collections.Generic.Dictionary<string, object>;
using Butterfly.Database.Dynamic;

namespace Butterfly.Database {
    /// <summary>
    /// Allows executing SELECT statements, creating transactions to execute INSERT, UPDATE, and DELETE statements; 
    /// creating dynamic views; and receiving data change events both on tables and dynamic views.<para/>
    /// 
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
    ///         await database.InsertAndCommitAsync("employee", values: {
    ///             department_id: 1,
    ///             name: "SpongeBob"
    ///         });
    ///         await database.InsertAndCommitAsync("employee", values: {
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
    ///     await database.InsertAndCommitAsync("employee", values: {
    ///         department_id: 1
    ///         name: "Mr Crabs"
    ///     });
    ///     
    ///     // This will NOT cause the above DynamicViewSet to echo an INSERT data event
    ///     // (because the department_id doesn't match)
    ///     await database.InsertAndCommitAsync("employee", values: {
    ///         department_id: 2
    ///         name: "Patrick Star"
    ///     });
    /// </code>
    /// </summary>
    public interface IDatabase {

        /// <summary>
        /// Dictionary of tables keyed by name
        /// </summary>
        Dictionary<string, Table> Tables { get; }

        /// <summary>
        /// Creates database tables from an embedded resource file by internally calling CreateFromTextAsync with the contents of the embedded resource file (<see cref="CreateFromTextAsync(string)"/>.
        /// </summary>
        /// <param name="assembly"></param>
        /// <param name="resourceFile"></param>
        /// <returns></returns>
        Task CreateFromResourceFileAsync(Assembly assembly, string resourceFile);

        /// <summary>
        /// Creates database tables from a string containing a semicolon delimited series of CREATE statements in MySQL format (will be converted to native database format as appropriate).<para/>
        /// Comments (lines beginning with --) will be ignored.<para/>
        /// Each CREATE statement must include a PRIMARY KEY definition.<para/>
        /// If the table already exists, the CREATE statement is ignored.<para/>
        /// Creating your database tables with this method is not required to use the rest of the Butterfly framework (you can instead just load your schema from your existing database using <see cref="LoadSchemaAsync"/>.
        /// </summary>
        /// <param name="createStatements"></param>
        Task CreateFromTextAsync(string createStatements);

        /// <summary>
        /// Adds a listener that is invoked when there is a new uncommitted transaction
        /// </summary>
        /// <param name="listener">The lambda to call when there is a new uncommitted <see cref="DataEventTransaction"/></param>
        /// <returns>An <see cref="IDisposable"/> that allows removing the listener by calling Dispose()</returns>
        IDisposable OnNewUncommittedTransaction(Action<DataEventTransaction> listener);

        /// <summary>
        /// Adds a listener that is invoked when there is a new committed transaction
        /// </summary>
        /// <param name="listener">The lambda to call when there is a new committed <see cref="DataEventTransaction"/></param>
        /// <returns>An <see cref="IDisposable"/> that allows removing the listener by calling Dispose()</returns>
        IDisposable OnNewCommittedTransaction(Action<DataEventTransaction> listener);

        /// <summary>
        /// Execute the SELECT statement and return the data in a <see cref="DataEventTransaction"/>
        /// </summary>
        /// <param name="selectStatement">A SELECT statement defining what data to return (can include parameters like @name)</param>
        /// <param name="vars">Either an anonymous type or Dictionary with the vars used in the SELECT statement</param>
        /// <returns>A <see cref="DataEventTransaction"/> with the returned data represented as a sequence of <see cref="DataEvent"/></returns>
        Task<DataEventTransaction> GetInitialDataEventTransactionAsync(string selectStatement, dynamic vars = null);

        /// <summary>
        /// Executes the SELECT statement of the DynamicQuery and returns a sequence of DataChange events starting an InitialBegin event, then an Insert event for each row, and then an InitialEnd event.
        /// </summary>
        /// <returns></returns>
        Task<DataEvent[]> GetInitialDataEventsAsync(string dataEventName, string[] keyFieldNames, SelectStatement selectStatement, dynamic statementParams = null);

        /// <summary>
        /// Executes the SELECT statement and return the value of the first column of the first row (the SELECT statement may contain vars like @name specified in <paramref name="vars"/>).<para/>
        /// 
        /// The WHERE clause will be rewritten to valid SQL in a few cases...
        /// - name=@name (rewritten to "name IS NULL" if @name equals null)
        /// - name!=@name (rewritten to "name IS NOT NULL" if @name equals null)
        /// - name=@name (rewritten to "1=2" if @name is an empty array)
        /// - name=@name (rewritten to "name='123' if @name is a single value array)
        /// - name=@name (rewriten to "name IN ('Jim', 'Bob') if @name is a multi value array)
        /// </summary>
        /// <typeparam name="T">The return type of the single value returned</typeparam>
        /// <param name="selectStatement">The SELECT statement to execute (may contain vars like @name specified in <paramref name="vars"/>)</param>
        /// <param name="vars">Either an anonymous type or Dictionary with the vars used in the SELECT statement</param>
        /// <param name="defaultValue">The value to return if no rows were returned or the value of the first column of the first row is null</param>
        /// <returns>The value of the first column of the first row</returns>
        Task<T> SelectValue<T>(string selectStatement, dynamic vars, T defaultValue);

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
        /// Executes the INSERT statement as a single transaction (the INSERT statement may contain vars like @name specified in <paramref name="vars"/>)
        /// </summary>
        /// <param name="insertStatement"></param>
        /// <param name="vars"></param>
        /// <param name="ignoreIfDuplicate"></param>
        /// <returns></returns>
        Task<object> InsertAndCommitAsync(string insertStatement, dynamic vars, bool ignoreIfDuplicate = false);

        /// <summary>
        /// Executes the UPDATE statement as a single transaction (the UPDATE statement may contain vars like @name specified in <paramref name="vars"/>)
        /// </summary>
        /// <param name="updateStatement"></param>
        /// <param name="vars"></param>
        /// <returns></returns>
        Task<int> UpdateAndCommitAsync(string updateStatement, dynamic vars);

        /// <summary>
        /// Executes the DELETE statement as a single transaction (the DELETE statement may contain vars like @name specified in <paramref name="vars"/>)
        /// </summary>
        /// <param name="deleteStatement"></param>
        /// <param name="vars"></param>
        /// <returns></returns>
        Task<int> DeleteAndCommitAsync(string deleteStatement, dynamic vars);

        /// <summary>
        /// Creates a new <see cref="ITransaction"/>.  Must execute <see cref="ITransaction.CommitAsync"/> to save the transaction changes.  Calling Dispose() on the transaction without committing rolls back the changes./>
        /// </summary>
        /// <returns>An <see cref="ITransaction"/> instance (can then call InsertAsync(), UpdateAsync(), or DeleteAsync() on the ITransaction instance to make changes on the transaction)/></returns>
        Task<ITransaction> BeginTransaction();

        /// <summary>
        /// Allows specifying a lambda that creates a default value for a field when executing an INSERT.  If the tableName parameter is left null, the getDefaultValue lambda will be applied to all tables.
        /// </summary>
        /// <param name="fieldName">Name of the field</param>
        /// <param name="getDefaultValue">The lambda that returns the default value</param>
        /// <param name="tableName">An optional table name.  If not null, the getDefaultValue lambda is only applied to the specified table. If null, the getDefaultValue lambda is applied to all tables.</param>
        void SetInsertDefaultValue(string fieldName, Func<object> getDefaultValue, string tableName = null);

        /// <summary>
        /// Allows creating a set of <see cref="DynamicView"/> instances that publish a single <see cref="DataEventTransaction"/> instance with initial data and new <see cref="DataEventTransaction"/> instances when data changes.  The <see cref="DataEventTransaction"/> instances are published to the lambda passed as the <paramref name="listener"/>.
        /// </summary>
        /// <param name="listener"></param>
        /// <returns></returns>
        DynamicViewSet CreateDynamicViewSet(Action<DataEventTransaction> listener);

        /// <summary>
        /// Allows creating a set of <see cref="DynamicView"/> instances that publish a single <see cref="DataEventTransaction"/> instance with initial data and new <see cref="DataEventTransaction"/> instances when data changes.  The <see cref="DataEventTransaction"/> instances are published to the lambda passed as the <paramref name="listener"/>.
        /// </summary>
        /// <param name="asyncListener"></param>
        /// <returns></returns>
        DynamicViewSet CreateDynamicViewSet(Func<DataEventTransaction, Task> asyncListener);

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

        /// <summary>
        /// Convenience method which creates a <see cref="DynamicViewSet"/>, adds a single <see cref="DynamicView"/> to the <see cref="DynamicViewSet"/>, and starts the <see cref="DynamicViewSet"/>.
        /// </summary>
        /// <param name="selectStatement"></param>
        /// <param name="asyncListener"></param>
        /// <param name="values"></param>
        /// <param name="name"></param>
        /// <param name="keyFieldNames"></param>
        /// <returns></returns>
        Task<DynamicViewSet> CreateAndStartDynamicView(string selectStatement, Func<DataEventTransaction, Task> asyncListener, dynamic values = null, string name = null, string[] keyFieldNames = null);
    }
}
