using JJCore.Domain.Enums;
using JJCore.Domain.Interfaces.Repository;
using JJCore.Infra.Data.Extensions;
using System.Data;

namespace JJCore.Infra.Data.Infrastructure
{
    public abstract class Repository<TEntity> : IRepository<TEntity> where TEntity : class
    {
        protected readonly IUnitOfWork UnitOfWork;

        public Repository(IUnitOfWork unitOfWork)
        {
            UnitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public virtual async Task<int> AddAsync(TEntity entity, IDbTransaction? transaction = null)
        {
            return await UnitOfWork.Connection.AddAsync(entity, transaction ?? UnitOfWork.Transaction);
        }

        public virtual async Task<object?> AddReturningAsync(TEntity entity, IDbTransaction? transaction = null)
        {
            return await UnitOfWork.Connection.AddReturningAsync(entity, transaction ?? UnitOfWork.Transaction);
        }

        public virtual async Task<int> AddBatchAsync(IEnumerable<TEntity> entities, IDbTransaction? transaction = null)
        {
            return await UnitOfWork.Connection.AddBatchAsync(entities, transaction ?? UnitOfWork.Transaction);
        }

        public virtual async Task<int> UpdateAsync(TEntity entity, IDbTransaction? transaction = null)
        {
            return await UnitOfWork.Connection.UpdateAsync(entity, transaction ?? UnitOfWork.Transaction);
        }

        public virtual async Task<int> DeleteAsync(int id, IDbTransaction? transaction = null)
        {
            return await UnitOfWork.Connection.DeleteAsync<TEntity>(id, transaction ?? UnitOfWork.Transaction);
        }

        public virtual async Task<int> DeleteAsync(long id, IDbTransaction? transaction = null)
        {
            return await UnitOfWork.Connection.DeleteAsync<TEntity>(id, transaction ?? UnitOfWork.Transaction);
        }

        public virtual async Task<int> DeleteAllAsync(IDbTransaction? transaction = null)
        {
            return await UnitOfWork.Connection.DeleteAllAsync<TEntity>(transaction ?? UnitOfWork.Transaction);
        }

        public virtual async Task<int> ExecuteQueryAsync(string query, object? parameters = null, IDbTransaction? transaction = null)
        {
            return await UnitOfWork.Connection.ExecuteQueryAsync(query, parameters, transaction ?? UnitOfWork.Transaction);
        }

        public virtual async Task<TEntity?> GetAsync(object id)
        {
            return await UnitOfWork.Connection.GetAsync<TEntity>(id, UnitOfWork.Transaction);
        }

        public virtual async Task<TEntity?> GetAsync(string condition, object? parameters = null)
        {
            return await UnitOfWork.Connection.GetAsync<TEntity>(condition, parameters, UnitOfWork.Transaction);
        }

        public virtual async Task<IEnumerable<TEntity>> GetListAsync(string condition = "", object? parameters = null)
        {
            return await UnitOfWork.Connection.GetListAsync<TEntity>(condition, parameters, UnitOfWork.Transaction);
        }

        public virtual async Task<long> GetNextCodeAsync(long userId, long initialValue = 1)
        {
            return await UnitOfWork.Connection.GetNextCodeAsync<TEntity>(userId, initialValue, UnitOfWork.Transaction);
        }

        public virtual async Task<IEnumerable<TEntity>> GetListByUserAsync(long userId)
        {
            return await UnitOfWork.Connection.GetListByUserAsync<TEntity>(userId, UnitOfWork.Transaction);
        }

        public virtual async Task<TEntity?> GetByCodeAsync(long code, long userId)
        {
            return await UnitOfWork.Connection.GetByCodeAsync<TEntity>(code, userId, UnitOfWork.Transaction);
        }

        public DatabaseType ActiveDatabase => UnitOfWork.Connection.IdentifyDatabase();
    }
}
