using JJCore.Domain.Enumeradores;
using System.Data;

namespace JJCore.Domain.Interfaces.Repository
{
    public interface IRepository<TEntity> where TEntity : class
    {
        Task<TEntity?> ObterAsync(object id);
        Task<TEntity?> ObterAsync(string condicao, object? parametros = null);
        Task<IEnumerable<TEntity>> ObterListaAsync(string condicao = "", object? parametros = null);
        Task<int> AdicionarEmLoteAsync(IEnumerable<TEntity> entidades, IDbTransaction? transaction = null);
        Task<int> AdicionarAsync(TEntity entity, IDbTransaction? transaction = null);
        Task<object?> AdicionarRetornandoAsync(TEntity entity, IDbTransaction? transaction = null);
        Task<int> AtualizarAsync(TEntity entity, IDbTransaction? transaction = null);
        Task<int> DeletarAsync(int id, IDbTransaction? transaction = null);
        Task<int> DeletarAsync(long id, IDbTransaction? transaction = null);
        Task<int> DeletarTudoAsync(IDbTransaction? transaction = null);
        Task<int> ExecutarQueryAsync(string query, object? parametros = null, IDbTransaction? transaction = null);
        Task<long> ObterProximoCodigoAsync(long usuarioID, long valorInicial = 1);
        Task<IEnumerable<TEntity>> ObterListaPorUsuarioAsync(long usuarioID);
        Task<TEntity?> ObterPorCodigoAsync(long codigo, long usuarioID);
        eTipoBancoDados BancoDadosAtivo { get; }
    }
}
