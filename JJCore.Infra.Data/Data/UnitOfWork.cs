using JJCore.Domain.Interfaces.Repository;
using System.Data;

namespace JJCore.Infra.Data.Data
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly IDbConnection _connection;
        private IDbTransaction? _transaction;
        private bool _disposed;

        public UnitOfWork(IDbConnection connection)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));

            if (_connection.State != ConnectionState.Open)
            {
                _connection.Open();
            }
        }

        public IDbConnection Connection => _connection;
        public IDbTransaction? Transaction => _transaction;

        public void BeginTransaction()
        {
            if (_transaction != null)
            {
                throw new InvalidOperationException("There is already an open transaction.");
            }

            _transaction = _connection.BeginTransaction();
        }

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
                _transaction.Rollback();
                throw;
            }
            finally
            {
                _transaction.Dispose();
                _transaction = null;
            }
        }

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
    }
}
