using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

using Butterfly.Database.Event;

using Dict = System.Collections.Generic.Dictionary<string, object>;

namespace Butterfly.Database {
    /// <summary>
    /// Allows executing INSERT, UPDATE, and DELETE statements; creating dynamic views; and receiving data
    /// change events both on tables and dynamic views
    /// 
    /// Adding records and echoing all data change events to the console...<para/>
    /// <code>
    ///     // Create database instance (will also read the schema from the database)
    ///     var database = new SomeDatabase();
    ///     
    ///     // Listen for all database data events
    ///     var databaseListener = database.OnNewCommittedTransaction(dataEventTransaction => {
    ///         console.WriteLine($"Low Level DataEventTransaction={dataEventTransaction}");
    ///     }) {
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
    ///     var dynamicView = database.CreateDynamicView(
    ///         "SELECT * FROM employee WHERE department_id=@departmentId", 
    ///         new {
    ///             departmentId = 1
    ///         },
    ///         dataEventTransaction => {
    ///             Console.WriteLine(dataEventTransaction);
    ///         }
    ///     );
    /// 
    ///     // This will cause the above DynamicView to echo an INSERT data event
    ///     await database.InsertAndCommitAsync("employee", values: {
    ///         department_id: 1
    ///         name: "Mr Crabs"
    ///     });
    ///     
    ///     // This will NOT cause the above DynamicView to echo an INSERT data event
    ///     // (because the department_id doesn't match)
    ///     await database.InsertAndCommitAsync("employee", values: {
    ///         department_id: 2
    ///         name: "Mr Crabs"
    ///     });
    /// </code>
    /// </summary>
    public interface IDatabase {

        string ConnectionString { get; }
        Dictionary<string, Table> Tables { get; }

        /// <summary>
        /// Creates database tables from an embedded resource file by internally calling CreateFromTextAsync with the contents of the embedded resource file (<see cref="CreateFromTextAsync(string)"/>.
        /// </summary>
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
        /// <param name="resourceFile"></param>
        /// <returns></returns>
        Task CreateFromTextAsync(string sql);

        IDisposable OnNewUncommittedTransaction(Action<DataEventTransaction> listener);
        IDisposable OnNewUncommittedTransaction(Func<DataEventTransaction, Task> listener);
        IDisposable OnNewCommittedTransaction(Action<DataEventTransaction> listener);
        IDisposable OnNewCommittedTransaction(Func<DataEventTransaction, Task> listener);

        Task<DataEventTransaction> GetInitialDataEventTransactionAsync(string statementSql, dynamic statementParams = null);

        /// <summary>
        /// Executes the select statement of the DynamicQuery and returns a sequence of DataChange events starting an InitialBegin event, then an Insert event for each row, and then an InitialEnd event.
        /// </summary>
        /// <returns></returns>
        Task<DataEvent[]> GetInitialDataEventsAsync(string dataEventName, string[] keyFieldNames, SelectStatement selectStatement, dynamic statementParams = null);

        Task<T> SelectValue<T>(string sql, dynamic values, T defaultValue);
        Task<Dict> SelectRowAsync(string statementSql, dynamic statementParams = null);
        Task<Dict[]> SelectRowsAsync(string statementSql, dynamic statementParams = null);
        Task<Dict[]> SelectRowsAsync(SelectStatement statement, dynamic statementParams, string newAndCondition, Dict newWhereParams);
        Task<Dict[]> SelectRowsAsync(SelectStatement statement, dynamic statementParams);

        Task<object> InsertAndCommitAsync(string sql, dynamic record, bool ignoreIfDuplicate = false);
        Task<int> UpdateAndCommitAsync(string sourceSql, dynamic sourceParams);
        Task<int> DeleteAndCommitAsync(string sql, dynamic whereParams);

        Task<ITransaction> BeginTransaction();

        void SetDefaultValue(string fieldName, Func<object> getDefaultValue, string tableName = null);

        Dict ApplyDefaultValues(Table table, Dict values);

    }
}
