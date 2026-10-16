using JJCore.Domain.Interfaces.Repository;
using System.Data;
using System.Data.Common;

namespace JJCore.Infra.Data.Data
{
    /// <summary>
    /// Manages one database connection and transaction with asynchronous lifecycle operations.
    /// </summary>
    public sealed class AsyncUnitOfWork : IAsyncUnitOfWork
    {
        private readonly DbConnection _connection;
        private DbTransaction? _transaction;
        private bool _disposed;

        /// <summary>
        /// Creates an asynchronous unit of work without opening the provided connection.
        /// </summary>
        /// <param name="connection">The database connection to manage.</param>
        public AsyncUnitOfWork(IDbConnection connection)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            _connection = connection as DbConnection
                ?? throw new ArgumentException("The connection must inherit from DbConnection.", nameof(connection));
        }

        /// <summary>
        /// Gets the managed database connection.
        /// </summary>
        public IDbConnection Connection => _connection;

        /// <summary>
        /// Gets the current transaction, when one is open.
        /// </summary>
        public IDbTransaction? Transaction => _transaction;

        /// <inheritdoc />
        public async Task OpenAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            // Some providers return an idle pooled connection without checking the token.
            cancellationToken.ThrowIfCancellationRequested();

            if (_connection.State != ConnectionState.Open)
            {
                await _connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public void BeginTransaction()
        {
            ThrowIfDisposed();
            EnsureNoOpenTransaction();

            if (_connection.State != ConnectionState.Open)
            {
                _connection.Open();
            }

            _transaction = _connection.BeginTransaction();
        }

        /// <inheritdoc />
        public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            EnsureNoOpenTransaction();
            await OpenAsync(cancellationToken).ConfigureAwait(false);

            // Some providers start the transaction without checking the token.
            cancellationToken.ThrowIfCancellationRequested();

#if NET8_0_OR_GREATER
            _transaction = await _connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
#else
            _transaction = _connection.BeginTransaction();
#endif
        }

        /// <inheritdoc />
        public void Commit()
        {
            ThrowIfDisposed();
            EnsureOpenTransaction();

            try
            {
                _transaction!.Commit();
            }
            catch
            {
                TryRollback();
                throw;
            }
            finally
            {
                DisposeTransaction();
            }
        }

        /// <inheritdoc />
        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            EnsureOpenTransaction();

            try
            {
#if NET8_0_OR_GREATER
                await _transaction!.CommitAsync(cancellationToken).ConfigureAwait(false);
#else
                cancellationToken.ThrowIfCancellationRequested();
                _transaction!.Commit();
#endif
            }
            catch
            {
                await TryRollbackAsync().ConfigureAwait(false);
                throw;
            }
            finally
            {
                await DisposeTransactionAsync().ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public void Rollback()
        {
            ThrowIfDisposed();

            if (_transaction == null)
            {
                return;
            }

            try
            {
                _transaction.Rollback();
            }
            finally
            {
                DisposeTransaction();
            }
        }

        /// <inheritdoc />
        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (_transaction == null)
            {
                return;
            }

            try
            {
#if NET8_0_OR_GREATER
                await _transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
#else
                cancellationToken.ThrowIfCancellationRequested();
                _transaction.Rollback();
#endif
            }
            finally
            {
                await DisposeTransactionAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Releases the current transaction and connection synchronously.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                DisposeTransaction();
            }
            finally
            {
                _connection.Dispose();
                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        /// Releases the current transaction and connection asynchronously when supported.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                await DisposeTransactionAsync().ConfigureAwait(false);
            }
            finally
            {
#if NET8_0_OR_GREATER
                await _connection.DisposeAsync().ConfigureAwait(false);
#else
                if (_connection is IAsyncDisposable asyncDisposable)
                {
                    await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                }
                else
                {
                    _connection.Dispose();
                }
#endif
                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }

        private void EnsureNoOpenTransaction()
        {
            if (_transaction != null)
            {
                throw new InvalidOperationException("There is already an open transaction.");
            }
        }

        private void EnsureOpenTransaction()
        {
            if (_transaction == null)
            {
                throw new InvalidOperationException("There is no open transaction to commit.");
            }
        }

        private void TryRollback()
        {
            try
            {
                _transaction?.Rollback();
            }
            catch
            {
            }
        }

#if NET8_0_OR_GREATER
        private async Task TryRollbackAsync()
        {
            try
            {
                if (_transaction != null)
                {
                    await _transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
                }
            }
            catch
            {
            }
        }
#else
        private Task TryRollbackAsync()
        {
            TryRollback();
            return Task.CompletedTask;
        }
#endif

        private void DisposeTransaction()
        {
            var transaction = _transaction;
            _transaction = null;
            transaction?.Dispose();
        }

        private async ValueTask DisposeTransactionAsync()
        {
            if (_transaction == null)
            {
                return;
            }

            var transaction = _transaction;
            _transaction = null;

#if NET8_0_OR_GREATER
            await transaction.DisposeAsync().ConfigureAwait(false);
#else
            if (transaction is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
            }
            else
            {
                transaction.Dispose();
            }
#endif
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(AsyncUnitOfWork));
            }
        }
    }
}
