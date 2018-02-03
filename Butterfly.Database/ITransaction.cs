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
using System.Threading.Tasks;

using Dict = System.Collections.Generic.Dictionary<string, object>;

namespace Butterfly.Database {
    /// <summary>
    /// Allows executing a series of INSERT, UPDATE, and DELETE actions atomically and publishing 
    /// a single <see cref="DataEventTransaction"/> on the underlying <see cref="IDatabase"/> instance
    /// when the transaction is committed.<para/>
    /// 
    /// Must call <see cref="Commit"/> or <see cref="CommitAsync"/> to have the changes committed.<para/>
    /// 
    /// If the transaction is disposed without calling <see cref="Commit"/> or <see cref="CommitAsync"/> the transaction is automatically rolled back.
    /// </summary>
    public interface ITransaction : IDisposable {

        IDatabase Database { get; }

        /// <summary>
        /// Executes an INSERT statement within this transaction
        /// </summary>
        /// <remarks>
        /// Do an INSERT using the table name and an anonymous type...
        /// <code>
        /// await transaction.InsertAsync("message", new {
        ///     text = "Hello",
        ///     owner_id = "123",
        /// });
        /// </code>
        /// Do an INSERT using a full statement and a Dictionary...
        /// <code>
        /// await transaction.InsertAsync("INSERT INTO message (text, owner_id) VALUES (@t, @oid)", new Dictionary&lt;string, object&gt; {
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
        Task<T> InsertAsync<T>(string insertStatement, dynamic vars, bool ignoreIfDuplicate = false);

        /// <summary>
        /// Executes an UPDATE statement within this transaction
        /// </summary>
        /// <remarks>
        /// Do an UPDATE using the table name and an anonymous type...
        /// <code>
        /// await database.UpdateAsync("message", new {
        ///     id = 123,
        ///     text = "Hello",
        /// });
        /// </code>
        /// Do an UPDATE using a full statement and a Dictionary...
        /// <code>
        /// await database.UpdateAsync("UPDATE message SET text=@t WHERE id=@id", new Dictionary&lt;string, object&gt; {
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
        Task<int> UpdateAsync(string updateStatement, dynamic vars);

        /// <summary>
        /// Executes a DELETE statement within this transaction
        /// </summary>
        /// <remarks>
        /// Do a DELETE using the table name and an anonymous type...
        /// <code>
        /// await database.DeleteAsync("message", new {
        ///     id = 123
        /// });
        /// </code>
        /// Do a DELETE using a full statement and a Dictionary...
        /// <code>
        /// await database.DeleteAsync("DELETE FROM message WHERE id=@id", new Dictionary&lt;string, object&gt; {
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
        Task<int> DeleteAsync(string deleteStatement, dynamic vars);

        Task<bool> Synchronize(string tableName, Dict[] existingRecords, Dict[] newRecords, string[] keyFieldNames = null);

        /// <summary>
        /// Truncate a table (deletes all records)
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        Task TruncateAsync(string tableName);

        /// <summary>
        /// Commit the transaction
        /// </summary>
        void Commit();

        /// <inheritdoc cref="Commit"/>
        Task CommitAsync();

        /// <summary>
        /// Rollback the transaction (called automatically if transaction is disposed without calling <see cref="Commit"/> or <see cref="CommitAsync"/>)
        /// </summary>
        void Rollback();
    }

}
