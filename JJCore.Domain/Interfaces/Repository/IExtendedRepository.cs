using JJCore.Domain.Models;
using System.Data;

namespace JJCore.Domain.Interfaces.Repository
{
    /// <summary>
    /// Extends the cancellable repository with deletion by any key type, overloads that take only a condition and a
    /// cancellation token, and SQL that is sent without identifier translation.
    /// </summary>
    /// <remarks>
    /// The members of this interface were added after <see cref="IRepository{TEntity}"/> and
    /// <see cref="ICancellableRepository{TEntity}"/> were published, so that classes implementing those interfaces keep
    /// compiling. Depend on this interface to use them through dependency injection.
    /// </remarks>
    /// <typeparam name="TEntity">The mapped entity type.</typeparam>
    public interface IExtendedRepository<TEntity> : ICancellableRepository<TEntity> where TEntity : class
    {
        /// <summary>
        /// Deletes one entity by a primary key of any type, such as Guid or string.
        /// </summary>
        /// <param name="id">The primary key value.</param>
        /// <param name="transaction">The transaction to use, when provided.</param>
        /// <returns>The number of affected rows.</returns>
        Task<int> DeleteAsync(object id, IDbTransaction? transaction = null);

        /// <summary>
        /// Deletes one entity by a primary key of any type with cancellation support.
        /// </summary>
        /// <param name="id">The primary key value.</param>
        /// <param name="transaction">The transaction to use, when provided.</param>
        /// <param name="cancellationToken">The token used to cancel the operation.</param>
        /// <returns>The number of affected rows.</returns>
        Task<int> DeleteAsync(object id, IDbTransaction? transaction, CancellationToken cancellationToken);

        /// <summary>
        /// Gets one entity using a custom WHERE condition without parameters, with cancellation support.
        /// </summary>
        /// <param name="condition">The SQL WHERE condition without the WHERE keyword.</param>
        /// <param name="cancellationToken">The token used to cancel the operation.</param>
        /// <returns>The entity when found; otherwise, null.</returns>
        Task<TEntity?> GetByConditionAsync(string condition, CancellationToken cancellationToken);

        /// <summary>
        /// Gets one entity using a WHERE condition that is sent without identifier translation.
        /// </summary>
        /// <param name="condition">The SQL WHERE condition without the WHERE keyword, written with native identifier quotes.</param>
        /// <param name="parameters">The query parameters.</param>
        /// <returns>The entity when found; otherwise, null.</returns>
        Task<TEntity?> GetByConditionAsync(RawSql condition, object? parameters = null);

        /// <summary>
        /// Gets one entity using a WHERE condition that is sent without identifier translation, with cancellation support.
        /// </summary>
        /// <param name="condition">The SQL WHERE condition without the WHERE keyword, written with native identifier quotes.</param>
        /// <param name="parameters">The query parameters.</param>
        /// <param name="cancellationToken">The token used to cancel the operation.</param>
        /// <returns>The entity when found; otherwise, null.</returns>
        Task<TEntity?> GetByConditionAsync(RawSql condition, object? parameters, CancellationToken cancellationToken);

        /// <summary>
        /// Gets a list of entities using an optional WHERE condition without parameters, with cancellation support.
        /// </summary>
        /// <param name="condition">The SQL WHERE condition without the WHERE keyword.</param>
        /// <param name="cancellationToken">The token used to cancel the operation.</param>
        /// <returns>The matching entities.</returns>
        Task<IEnumerable<TEntity>> GetListAsync(string condition, CancellationToken cancellationToken);

        /// <summary>
        /// Gets a list of entities using a WHERE condition that is sent without identifier translation.
        /// </summary>
        /// <param name="condition">The SQL WHERE condition without the WHERE keyword, written with native identifier quotes.</param>
        /// <param name="parameters">The query parameters.</param>
        /// <returns>The matching entities.</returns>
        Task<IEnumerable<TEntity>> GetListAsync(RawSql condition, object? parameters = null);

        /// <summary>
        /// Gets a list of entities using a WHERE condition that is sent without identifier translation, with cancellation support.
        /// </summary>
        /// <param name="condition">The SQL WHERE condition without the WHERE keyword, written with native identifier quotes.</param>
        /// <param name="parameters">The query parameters.</param>
        /// <param name="cancellationToken">The token used to cancel the operation.</param>
        /// <returns>The matching entities.</returns>
        Task<IEnumerable<TEntity>> GetListAsync(RawSql condition, object? parameters, CancellationToken cancellationToken);

        /// <summary>
        /// Executes a custom SQL command without parameters, with cancellation support.
        /// </summary>
        /// <param name="query">The SQL command to execute.</param>
        /// <param name="cancellationToken">The token used to cancel the operation.</param>
        /// <returns>The number of affected rows.</returns>
        Task<int> ExecuteQueryAsync(string query, CancellationToken cancellationToken);

        /// <summary>
        /// Executes a custom SQL command that is sent without identifier translation.
        /// </summary>
        /// <param name="query">The SQL command to execute, written with native identifier quotes.</param>
        /// <param name="parameters">The command parameters.</param>
        /// <param name="transaction">The transaction to use, when provided.</param>
        /// <returns>The number of affected rows.</returns>
        Task<int> ExecuteQueryAsync(RawSql query, object? parameters = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Executes a custom SQL command that is sent without identifier translation, with cancellation support.
        /// </summary>
        /// <param name="query">The SQL command to execute, written with native identifier quotes.</param>
        /// <param name="parameters">The command parameters.</param>
        /// <param name="transaction">The transaction to use, when provided.</param>
        /// <param name="cancellationToken">The token used to cancel the operation.</param>
        /// <returns>The number of affected rows.</returns>
        Task<int> ExecuteQueryAsync(RawSql query, object? parameters, IDbTransaction? transaction, CancellationToken cancellationToken);
    }
}
