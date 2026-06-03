using System.Data;

namespace JJCore.Domain.Interfaces.Repository
{
    /// <summary>
    /// Represents a database unit of work with one connection and an optional transaction.
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        /// <summary>
        /// Gets the active database connection.
        /// </summary>
        IDbConnection Connection { get; }

        /// <summary>
        /// Gets the current database transaction, when one is open.
        /// </summary>
        IDbTransaction? Transaction { get; }

        /// <summary>
        /// Opens a new database transaction.
        /// </summary>
        void BeginTransaction();

        /// <summary>
        /// Commits the current database transaction.
        /// </summary>
        void Commit();

        /// <summary>
        /// Rolls back the current database transaction.
        /// </summary>
        void Rollback();
    }
}
