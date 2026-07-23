using JJCore.Domain.Enums;
using System.Data;

namespace JJCore.Domain.Interfaces.Repository
{
    /// <summary>
    /// Defines generic asynchronous CRUD operations for a mapped entity.
    /// </summary>
    /// <typeparam name="TEntity">The mapped entity type.</typeparam>
    public interface IRepository<TEntity> where TEntity : class
    {
        /// <summary>
        /// Gets one entity by its primary key value.
        /// </summary>
        /// <param name="id">The primary key value.</param>
        /// <returns>The entity when found; otherwise, null.</returns>
        Task<TEntity?> GetAsync(object id);

        /// <summary>
        /// Gets one entity using a custom WHERE condition.
        /// </summary>
        /// <param name="condition">The SQL WHERE condition without the WHERE keyword.</param>
        /// <param name="parameters">The query parameters.</param>
        /// <returns>The entity when found; otherwise, null.</returns>
        Task<TEntity?> GetAsync(string condition, object? parameters = null);

        /// <summary>
        /// Gets a list of entities using an optional WHERE condition.
        /// </summary>
        /// <param name="condition">The SQL WHERE condition without the WHERE keyword.</param>
        /// <param name="parameters">The query parameters.</param>
        /// <returns>The matching entities.</returns>
        Task<IEnumerable<TEntity>> GetListAsync(string condition = "", object? parameters = null);

        /// <summary>
        /// Inserts multiple entities using one generated INSERT statement.
        /// </summary>
        /// <param name="entities">The entities to insert.</param>
        /// <param name="transaction">The transaction to use, when provided.</param>
        /// <returns>The number of affected rows.</returns>
        Task<int> AddBatchAsync(IEnumerable<TEntity> entities, IDbTransaction? transaction = null);

        /// <summary>
        /// Inserts one entity and returns the generated primary key as an integer.
        /// </summary>
        /// <param name="entity">The entity to insert.</param>
        /// <param name="transaction">The transaction to use, when provided.</param>
        /// <returns>The generated primary key converted to int.</returns>
        Task<int> AddAsync(TEntity entity, IDbTransaction? transaction = null);

        /// <summary>
        /// Inserts one entity and returns the generated primary key without forcing an integer conversion.
        /// </summary>
        /// <param name="entity">The entity to insert.</param>
        /// <param name="transaction">The transaction to use, when provided.</param>
        /// <returns>The generated primary key value.</returns>
        Task<object?> AddReturningAsync(TEntity entity, IDbTransaction? transaction = null);

        /// <summary>
        /// Updates one entity using its mapped primary key.
        /// </summary>
        /// <param name="entity">The entity to update.</param>
        /// <param name="transaction">The transaction to use, when provided.</param>
        /// <returns>The number of affected rows.</returns>
        Task<int> UpdateAsync(TEntity entity, IDbTransaction? transaction = null);

        /// <summary>
        /// Deletes one entity by an integer primary key.
        /// </summary>
        /// <param name="id">The primary key value.</param>
        /// <param name="transaction">The transaction to use, when provided.</param>
        /// <returns>The number of affected rows.</returns>
        Task<int> DeleteAsync(int id, IDbTransaction? transaction = null);

        /// <summary>
        /// Deletes one entity by a long primary key.
        /// </summary>
        /// <param name="id">The primary key value.</param>
        /// <param name="transaction">The transaction to use, when provided.</param>
        /// <returns>The number of affected rows.</returns>
        Task<int> DeleteAsync(long id, IDbTransaction? transaction = null);

        /// <summary>
        /// Deletes all rows from the mapped table.
        /// </summary>
        /// <param name="transaction">The transaction to use, when provided.</param>
        /// <returns>The number of affected rows.</returns>
        Task<int> DeleteAllAsync(IDbTransaction? transaction = null);

        /// <summary>
        /// Executes a custom SQL command with optional parameters.
        /// </summary>
        /// <param name="query">The SQL command to execute.</param>
        /// <param name="parameters">The command parameters.</param>
        /// <param name="transaction">The transaction to use, when provided.</param>
        /// <returns>The number of affected rows.</returns>
        Task<int> ExecuteQueryAsync(string query, object? parameters = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Gets the next sequential code for a user-owned entity.
        /// </summary>
        /// <param name="userId">The user identifier used to scope the sequence.</param>
        /// <param name="initialValue">The first value to use when no rows exist.</param>
        /// <returns>The next available code. The value is not reserved atomically and can collide under concurrent calls.</returns>
        Task<long> GetNextCodeAsync(long userId, long initialValue = 1);

        /// <summary>
        /// Gets all entities owned by one user.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <returns>The matching entities.</returns>
        Task<IEnumerable<TEntity>> GetListByUserAsync(long userId);

        /// <summary>
        /// Gets one user-owned entity by its per-user code.
        /// </summary>
        /// <param name="code">The per-user code value.</param>
        /// <param name="userId">The user identifier.</param>
        /// <returns>The entity when found; otherwise, null.</returns>
        Task<TEntity?> GetByCodeAsync(long code, long userId);

        /// <summary>
        /// Gets the database provider currently used by the repository connection.
        /// </summary>
        DatabaseType ActiveDatabase { get; }
    }
}
