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
