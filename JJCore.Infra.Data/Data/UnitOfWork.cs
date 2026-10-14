using JJCore.Domain.Interfaces.Repository;
using System.Data;

namespace JJCore.Infra.Data.Data
{
    /// <summary>
    /// Manages one database connection and an optional transaction scope.
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly IDbConnection _connection;
        private IDbTransaction? _transaction;
        private bool _disposed;

        /// <summary>
        /// Creates a unit of work and opens the provided connection when needed.
        /// </summary>
        /// <param name="connection">The database connection to manage.</param>
        public UnitOfWork(IDbConnection connection)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));

            if (_connection.State != ConnectionState.Open)
            {
                _connection.Open();
            }
        }

        /// <summary>
        /// Gets the managed database connection.
        /// </summary>
        public IDbConnection Connection => _connection;

        /// <summary>
        /// Gets the current transaction, when one is open.
        /// </summary>
        public IDbTransaction? Transaction => _transaction;

        /// <summary>
        /// Opens a new transaction for the managed connection.
        /// </summary>
        public void BeginTransaction()
        {
            if (_transaction != null)
            {
                throw new InvalidOperationException("There is already an open transaction.");
            }

            _transaction = _connection.BeginTransaction();
        }

        /// <summary>
        /// Commits the current transaction.
        /// </summary>
        public void Commit()
        {
            if (_transaction == null)
            {
                throw new InvalidOperationException("There is no open transaction to commit.");
            }

            try
            {
                _transaction.Commit();
            }
            catch
            {
                TryRollback();
                throw;
            }
            finally
            {
                _transaction.Dispose();
                _transaction = null;
            }
        }

        /// <summary>
        /// Rolls back the current transaction.
        /// </summary>
        public void Rollback()
        {
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
                _transaction.Dispose();
                _transaction = null;
            }
        }

        /// <summary>
        /// Releases the current transaction and connection.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _transaction?.Dispose();
            _connection.Dispose();
            _disposed = true;
            GC.SuppressFinalize(this);
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
    }
}
