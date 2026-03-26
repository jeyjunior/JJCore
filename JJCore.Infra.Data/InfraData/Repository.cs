using JJCore.Domain.Enumeradores;
using JJCore.Domain.Interfaces.Repositoy;
using JJCore.Infra.Data.Extensao;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JJCore.Infra.Data.InfraData
{
    public abstract class Repository<TEntity> : IRepository<TEntity> where TEntity : class
    {
        protected readonly IUnitOfWork unitOfWork;

        public Repository(IUnitOfWork unitOfWork)
        {
            this.unitOfWork = unitOfWork;
        }
        public virtual async Task<int> AdicionarAsync(TEntity entity, IDbTransaction transaction = null)
        {
            return await unitOfWork.Connection.AdicionarAsync<TEntity>(entity, transaction ?? unitOfWork.Transaction);
        }
        public virtual async Task<int> AdicionarEmLoteAsync(IEnumerable<TEntity> entidades, IDbTransaction transaction = null)
        {
            return await unitOfWork.Connection.AdicionarEmLoteAsync<TEntity>(entidades, transaction ?? unitOfWork.Transaction);
        }
        public virtual async Task<int> AtualizarAsync(TEntity entity, IDbTransaction transaction = null)
        {
            return await unitOfWork.Connection.AtualizarAsync<TEntity>(entity, transaction ?? unitOfWork.Transaction);
        }
        public virtual async Task<int> DeletarAsync(int id, IDbTransaction transaction = null)
        {
            return await unitOfWork.Connection.DeletarAsync<TEntity>(id, transaction ?? unitOfWork.Transaction);
        }
        public virtual async Task<int> DeletarAsync(long id, IDbTransaction transaction = null)
        {
            return await unitOfWork.Connection.DeletarAsync<TEntity>(id, transaction ?? unitOfWork.Transaction);
        }
        public virtual async Task<int> DeletarTudoAsync(IDbTransaction transaction = null)
        {
            return await unitOfWork.Connection.DeletarTudoAsync<TEntity>(transaction ?? unitOfWork.Transaction);
        }
        public virtual async Task<int> ExecutarQueryAsync(string query, object parametros = null, IDbTransaction transaction = null)
        {
            return await unitOfWork.Connection.ExecutarQueryAsync(query, parametros, transaction ?? unitOfWork.Transaction);
        }
        public virtual async Task<TEntity?> ObterAsync(object id)
        {
            return await unitOfWork.Connection.ObterAsync<TEntity>(id);
        }
        public virtual async Task<TEntity?> ObterAsync(string condicao, object parametros = null)
        {
            return await unitOfWork.Connection.ObterAsync<TEntity>(condicao, parametros);
        }
        public virtual async Task<IEnumerable<TEntity>> ObterListaAsync(string condicao = "", object parametros = null)
        {
            return await unitOfWork.Connection.ObterListaAsync<TEntity>(condicao, parametros);
        }
        public virtual async Task<long> ObterProximoCodigoAsync(long usuarioID, long valorInicial = 1)
        {
            return await unitOfWork.Connection.ObterProximoCodigoAsync<TEntity>(usuarioID, valorInicial, unitOfWork.Transaction);
        }
        public virtual async Task<IEnumerable<TEntity>> ObterListaPorUsuarioAsync(long usuarioID)
        {
            return await unitOfWork.Connection.ObterListaPorUsuarioAsync<TEntity>(usuarioID);
        }
        public virtual async Task<TEntity?> ObterPorCodigoAsync(long codigo, long usuarioID)
        {
            return await unitOfWork.Connection.ObterPorCodigoAsync<TEntity>(codigo, usuarioID, unitOfWork.Transaction);
        }
        public eTipoBancoDados BancoDadosAtivo
        {
            get => unitOfWork.Connection.IdentificarBanco();
        }
    }
}
