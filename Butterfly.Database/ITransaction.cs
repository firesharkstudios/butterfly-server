using System;
using System.Threading.Tasks;

namespace Butterfly.Database {
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
