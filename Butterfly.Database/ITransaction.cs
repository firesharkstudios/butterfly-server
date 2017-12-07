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
using System.Threading.Tasks;
using Butterfly.Database.Event;

namespace Butterfly.Database {
    /// <summary>
    /// Allows executing a series of INSERT, UPDATE, and DELETE actions atomically and publishing 
    /// a single <see cref="DataEventTransaction"/> on the underlying <see cref="IDatabase"/> instance
    /// when the transaction is committed.<para/>
    /// 
    /// Must call <see cref="CommitAsync"/> to have the changes committed.<para/>
    /// 
    /// If the transaction is disposed without calling <see cref="CommitAsync"/> the transaction is rolled back.
    /// </summary>
    public interface ITransaction : IDisposable {
        Task BeginAsync();

        Task<bool> CreateAsync(CreateStatement statement);

        Task<object> InsertAsync(string statementSql, dynamic statementParams, bool ignoreIfDuplicate = false);
        Task<object> InsertAsync(InsertStatement statement, dynamic statementParams, bool ignoreIfDuplicate = false);

        Task<int> UpdateAsync(string statementSql, dynamic statementParams);
        Task<int> UpdateAsync(UpdateStatement statement, dynamic statementParams);

        Task<int> DeleteAsync(string statementSql, dynamic statementParams);
        Task<int> DeleteAsync(DeleteStatement statement, dynamic statementParams);

        Task TruncateAsync(string tableName);

        Task CommitAsync();

        void Rollback();
    }

}
