using JJCore.Domain.Enums;
using System.Data;

namespace JJCore.Domain.Interfaces.Repository
{
    public interface IRepository<TEntity> where TEntity : class
    {
        Task<TEntity?> GetAsync(object id);
        Task<TEntity?> GetAsync(string condition, object? parameters = null);
        Task<IEnumerable<TEntity>> GetListAsync(string condition = "", object? parameters = null);
        Task<int> AddBatchAsync(IEnumerable<TEntity> entities, IDbTransaction? transaction = null);
        Task<int> AddAsync(TEntity entity, IDbTransaction? transaction = null);
        Task<object?> AddReturningAsync(TEntity entity, IDbTransaction? transaction = null);
        Task<int> UpdateAsync(TEntity entity, IDbTransaction? transaction = null);
        Task<int> DeleteAsync(int id, IDbTransaction? transaction = null);
        Task<int> DeleteAsync(long id, IDbTransaction? transaction = null);
        Task<int> DeleteAllAsync(IDbTransaction? transaction = null);
        Task<int> ExecuteQueryAsync(string query, object? parameters = null, IDbTransaction? transaction = null);
        Task<long> GetNextCodeAsync(long userId, long initialValue = 1);
        Task<IEnumerable<TEntity>> GetListByUserAsync(long userId);
        Task<TEntity?> GetByCodeAsync(long code, long userId);
        DatabaseType ActiveDatabase { get; }
    }
}
