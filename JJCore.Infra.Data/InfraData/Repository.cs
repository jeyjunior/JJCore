using JJCore.Domain.Enumeradores;
using JJCore.Domain.Interfaces.Repository;
using JJCore.Infra.Data.Extensao;
using System.Data;

namespace JJCore.Infra.Data.InfraData
{
    public abstract class Repository<TEntity> : IRepository<TEntity> where TEntity : class
    {
        protected readonly IUnitOfWork UnitOfWork;

        public Repository(IUnitOfWork unitOfWork)
        {
            UnitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public virtual async Task<int> AdicionarAsync(TEntity entity, IDbTransaction? transaction = null)
        {
            return await UnitOfWork.Connection.AdicionarAsync(entity, transaction ?? UnitOfWork.Transaction);
        }

        public virtual async Task<object?> AdicionarRetornandoAsync(TEntity entity, IDbTransaction? transaction = null)
        {
            return await UnitOfWork.Connection.AdicionarRetornandoAsync(entity, transaction ?? UnitOfWork.Transaction);
        }

        public virtual async Task<int> AdicionarEmLoteAsync(IEnumerable<TEntity> entidades, IDbTransaction? transaction = null)
        {
            return await UnitOfWork.Connection.AdicionarEmLoteAsync(entidades, transaction ?? UnitOfWork.Transaction);
        }

        public virtual async Task<int> AtualizarAsync(TEntity entity, IDbTransaction? transaction = null)
        {
            return await UnitOfWork.Connection.AtualizarAsync(entity, transaction ?? UnitOfWork.Transaction);
        }

        public virtual async Task<int> DeletarAsync(int id, IDbTransaction? transaction = null)
        {
            return await UnitOfWork.Connection.DeletarAsync<TEntity>(id, transaction ?? UnitOfWork.Transaction);
        }

        public virtual async Task<int> DeletarAsync(long id, IDbTransaction? transaction = null)
        {
            return await UnitOfWork.Connection.DeletarAsync<TEntity>(id, transaction ?? UnitOfWork.Transaction);
        }

        public virtual async Task<int> DeletarTudoAsync(IDbTransaction? transaction = null)
        {
            return await UnitOfWork.Connection.DeletarTudoAsync<TEntity>(transaction ?? UnitOfWork.Transaction);
        }

        public virtual async Task<int> ExecutarQueryAsync(string query, object? parametros = null, IDbTransaction? transaction = null)
        {
            return await UnitOfWork.Connection.ExecutarQueryAsync(query, parametros, transaction ?? UnitOfWork.Transaction);
        }

        public virtual async Task<TEntity?> ObterAsync(object id)
        {
            return await UnitOfWork.Connection.ObterAsync<TEntity>(id, UnitOfWork.Transaction);
        }

        public virtual async Task<TEntity?> ObterAsync(string condicao, object? parametros = null)
        {
            return await UnitOfWork.Connection.ObterAsync<TEntity>(condicao, parametros, UnitOfWork.Transaction);
        }

        public virtual async Task<IEnumerable<TEntity>> ObterListaAsync(string condicao = "", object? parametros = null)
        {
            return await UnitOfWork.Connection.ObterListaAsync<TEntity>(condicao, parametros, UnitOfWork.Transaction);
        }

        public virtual async Task<long> ObterProximoCodigoAsync(long usuarioID, long valorInicial = 1)
        {
            return await UnitOfWork.Connection.ObterProximoCodigoAsync<TEntity>(usuarioID, valorInicial, UnitOfWork.Transaction);
        }

        public virtual async Task<IEnumerable<TEntity>> ObterListaPorUsuarioAsync(long usuarioID)
        {
            return await UnitOfWork.Connection.ObterListaPorUsuarioAsync<TEntity>(usuarioID, UnitOfWork.Transaction);
        }

        public virtual async Task<TEntity?> ObterPorCodigoAsync(long codigo, long usuarioID)
        {
            return await UnitOfWork.Connection.ObterPorCodigoAsync<TEntity>(codigo, usuarioID, UnitOfWork.Transaction);
        }

        public eTipoBancoDados BancoDadosAtivo => UnitOfWork.Connection.IdentificarBanco();
    }
}
