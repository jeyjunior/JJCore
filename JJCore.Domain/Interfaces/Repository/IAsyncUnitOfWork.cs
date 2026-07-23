using System.Threading;

namespace JJCore.Domain.Interfaces.Repository
{
    /// <summary>
    /// Represents an asynchronously managed database unit of work.
    /// </summary>
    public interface IAsyncUnitOfWork : IUnitOfWork, IAsyncDisposable
    {
        /// <summary>
        /// Opens the database connection asynchronously when it is not already open.
        /// </summary>
        /// <param name="cancellationToken">The token used to cancel the operation.</param>
        Task OpenAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Opens a new database transaction asynchronously.
        /// </summary>
        /// <param name="cancellationToken">The token used to cancel the operation.</param>
        Task BeginTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Commits the current database transaction asynchronously.
        /// </summary>
        /// <param name="cancellationToken">The token used to cancel the operation.</param>
        Task CommitAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Rolls back the current database transaction asynchronously.
        /// </summary>
        /// <param name="cancellationToken">The token used to cancel the operation.</param>
        Task RollbackAsync(CancellationToken cancellationToken = default);
    }
}
