using JJCore.Domain.Atributos;
using JJCore.Domain.Enumeradores;
using JJCore.Infra.Data.Dicionario;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;

namespace JJCore.Infra.Data.Extensao
{
    public static class DapperExtension
    {
        #region SELECT 
        public static async Task<T?> ObterAsync<T>(this IDbConnection connection, object id)
        {
            var tipoEntidade = typeof(T);

            string nomeTabela = ObterNomeTabelaFormatado(tipoEntidade);
            string nomePK = PadronizarNome(ObterNomeChavePrimaria(tipoEntidade));

            string sql = $"SELECT {nomeTabela}.* FROM {nomeTabela} WHERE {nomePK} = @Id";

            sql = SQLTradutor.TraduzirQuery(sql, connection.IdentificarBanco());

            return await connection.QuerySingleOrDefaultAsync<T>(sql, new { Id = id });
        }

        public static async Task<T?> ObterAsync<T>(this IDbConnection connection, string condicao, object parametros = null)
        {
            var tipoEntidade = typeof(T);
            string nomeTabela = ObterNomeTabelaFormatado(tipoEntidade);

            if (string.IsNullOrWhiteSpace(condicao))
                throw new ArgumentException("Para buscar sem ID, a condição WHERE é obrigatória.", nameof(condicao));

            string sql = $"SELECT {nomeTabela}.* FROM {nomeTabela} WHERE {condicao}";

            sql = SQLTradutor.TraduzirQuery(sql, connection.IdentificarBanco());

            return await connection.QuerySingleOrDefaultAsync<T>(sql, parametros);
        }

        public static async Task<IEnumerable<T>> ObterListaAsync<T>(this IDbConnection connection, string condicao = "", object parametros = null)
        {
            var tipoEntidade = typeof(T);
            string nomeTabela = ObterNomeTabelaFormatado(tipoEntidade);

            string sql = $"SELECT {nomeTabela}.* FROM {nomeTabela}";

            if (!string.IsNullOrWhiteSpace(condicao))
                sql += $" WHERE {condicao}";

            sql = SQLTradutor.TraduzirQuery(sql, connection.IdentificarBanco());

            return await connection.QueryAsync<T>(sql, parametros);
        }
        #endregion

        #region INSERT
        public static async Task<int> AdicionarEmLoteAsync<T>(this IDbConnection connection, IEnumerable<T> entidades, IDbTransaction transaction = null)
        {
            if (entidades == null || !entidades.Any()) return 0;

            var tipoBanco = connection.IdentificarBanco();
            var entidade = typeof(T);

            string tabela = ObterNomeTabelaFormatado(entidade);
            var propriedades = ObterPropriedadesMapeaveis(entidade);

            var colunas = propriedades.Select(p => PadronizarNome(p.Name)).ToList();
            var paramNames = propriedades.Select(p => "@" + p.Name).ToList();

            string sql = $"INSERT INTO {tabela} ({string.Join(", ", colunas)}) VALUES ({string.Join(", ", paramNames)})";

            sql = SQLTradutor.TraduzirQuery(sql, tipoBanco);

            return await connection.ExecuteAsync(sql, entidades, transaction);
        }

        public static async Task<int> AdicionarAsync<T>(this IDbConnection connection, T entity, IDbTransaction transaction = null)
        {
            var tipoBanco = connection.IdentificarBanco();
            var entidade = typeof(T);

            string nomeTabela = ObterNomeTabelaFormatado(entidade);
            string nomePkSemFormatacao = ObterNomeChavePrimaria(entidade);
            string nomePkFormatado = PadronizarNome(nomePkSemFormatacao);

            var propriedades = ObterPropriedadesMapeaveis(entidade);
            var colunas = new List<string>();
            var paramNames = new List<string>();
            var parametros = new DynamicParameters();

            foreach (var propriedade in propriedades)
            {
                var valor = propriedade.GetValue(entity);

                if (propriedade.GetCustomAttribute<Obrigatorio>() != null && valor == null)
                    throw new InvalidOperationException($"A propriedade {propriedade.Name} é obrigatória e não foi preenchida.");

                if (propriedade.PropertyType == typeof(DateTime))
                {
                    if (valor == null || (DateTime)valor == DateTime.MinValue)
                        valor = DateTime.UtcNow;
                }

                colunas.Add(PadronizarNome(propriedade.Name));
                paramNames.Add("@" + propriedade.Name);
                parametros.Add(propriedade.Name, valor);
            }

            string cols = string.Join(", ", colunas);
            string vals = string.Join(", ", paramNames);
            string sql = "";

            // O Switch fica aqui apenas por causa da forma como cada banco retorna o ID inserido
            switch (tipoBanco)
            {
                case eTipoBancoDados.PostgreSQL:
                    sql = $"INSERT INTO {nomeTabela} ({cols}) VALUES ({vals}) RETURNING {nomePkFormatado};";
                    break;
                case eTipoBancoDados.SQLServer:
                    sql = $"INSERT INTO {nomeTabela} ({cols}) VALUES ({vals}); SELECT CAST(SCOPE_IDENTITY() as int);";
                    break;
                default:
                    sql = $"INSERT INTO {nomeTabela} ({cols}) VALUES ({vals}); SELECT last_insert_rowid();";
                    break;
            }

            sql = SQLTradutor.TraduzirQuery(sql, tipoBanco);

            var result = await connection.ExecuteScalarAsync<int>(sql, parametros, transaction);

            if (result == 0)
                throw new InvalidOperationException("Nenhuma linha foi inserida no banco de dados.");

            return result;
        }
        #endregion

        #region UPDATE
        public static async Task<int> AtualizarAsync<T>(this IDbConnection connection, T entity, IDbTransaction transaction = null)
        {
            var tipoBanco = connection.IdentificarBanco();
            var entidade = typeof(T);

            string nomeTabela = ObterNomeTabelaFormatado(entidade);
            string nomePkSemFormatacao = ObterNomeChavePrimaria(entidade);
            string nomePkFormatado = PadronizarNome(nomePkSemFormatacao);

            var propriedades = ObterPropriedadesMapeaveis(entidade);
            var colunasUpdate = new List<string>();
            var parametros = new DynamicParameters();

            var propPk = entidade.GetProperty(nomePkSemFormatacao);
            var valorPk = propPk?.GetValue(entity);

            foreach (var propriedade in propriedades)
            {
                var valor = propriedade.GetValue(entity);

                if (propriedade.GetCustomAttribute<Obrigatorio>() != null && valor == null)
                    throw new InvalidOperationException($"A propriedade {propriedade.Name} é obrigatória.");

                colunasUpdate.Add($"{PadronizarNome(propriedade.Name)} = @{propriedade.Name}");
                parametros.Add(propriedade.Name, valor);
            }

            parametros.Add("Id", valorPk);

            string sql = $"UPDATE {nomeTabela} SET {string.Join(", ", colunasUpdate)} WHERE {nomePkFormatado} = @Id";

            sql = SQLTradutor.TraduzirQuery(sql, tipoBanco);

            var rowsAffected = await connection.ExecuteAsync(sql, parametros, transaction);

            if (rowsAffected == 0)
                throw new InvalidOperationException("Nenhuma linha foi atualizada. Verifique se o ID existe.");

            return rowsAffected;
        }
        #endregion

        #region DELETE
        public static async Task<int> DeletarAsync<T>(this IDbConnection connection, object id, IDbTransaction transaction = null)
        {
            var tipoBanco = connection.IdentificarBanco();
            var entidade = typeof(T);

            string nomeTabela = ObterNomeTabelaFormatado(entidade);
            string nomePkFormatado = PadronizarNome(ObterNomeChavePrimaria(entidade));

            string sql = $"DELETE FROM {nomeTabela} WHERE {nomePkFormatado} = @Id";
            sql = SQLTradutor.TraduzirQuery(sql, tipoBanco);

            int linhas = await connection.ExecuteAsync(sql, new { Id = id }, transaction);

            if (linhas == 0)
                throw new InvalidOperationException("O registro a ser excluído não existe.");

            return linhas;
        }

        public static async Task<int> DeletarTudoAsync<T>(this IDbConnection connection, IDbTransaction transaction = null)
        {
            var tipoBanco = connection.IdentificarBanco();
            string nomeTabela = ObterNomeTabelaFormatado(typeof(T));

            string sql = $"DELETE FROM {nomeTabela}";
            sql = SQLTradutor.TraduzirQuery(sql, tipoBanco);

            return await connection.ExecuteAsync(sql, transaction: transaction);
        }
        #endregion 

        #region GENÉRICA
        public static async Task<int> ExecutarQueryAsync(this IDbConnection connection, string query, object parametros = null, IDbTransaction transaction = null)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentException("A query fornecida não pode ser nula ou vazia.");

            // A query genérica também entra na dança da tradução!
            query = SQLTradutor.TraduzirQuery(query, connection.IdentificarBanco());

            return await connection.ExecuteAsync(query, parametros, transaction);
        }
        #endregion

        #region MÉTODOS CUSTOMIZADOS
        public static async Task<long> ObterProximoCodigoAsync<T>(this IDbConnection connection, long usuarioID, long valorInicial = 1, IDbTransaction transaction = null)
        {
            var tipoBanco = connection.IdentificarBanco();
            var tipoEntidade = typeof(T);

            string tabela = ObterNomeTabelaFormatado(tipoEntidade);
            string colCodigo = PadronizarNome("Codigo");
            string colFK = PadronizarNome("FK_Usuario");

            long valorBase = valorInicial - 1;

            string sql = $"SELECT COALESCE(MAX({colCodigo}), @ValorBase) + 1 FROM {tabela} WHERE {colFK} = @UsuarioID";
            sql = SQLTradutor.TraduzirQuery(sql, tipoBanco);

            var result = await connection.ExecuteScalarAsync<object>(sql, new { UsuarioID = usuarioID, ValorBase = valorBase }, transaction);

            return Convert.ToInt64(result);
        }

        public static async Task<T?> ObterPorCodigoAsync<T>(this IDbConnection connection, long codigo, long usuarioID, IDbTransaction transaction = null)
        {
            string colCodigo = PadronizarNome("Codigo");
            string colFK_Usuario = PadronizarNome("FK_Usuario");

            string condicao = $"{colCodigo} = @Codigo AND {colFK_Usuario} = @UsuarioID";

            return await connection.ObterAsync<T>(condicao, new { Codigo = codigo, UsuarioID = usuarioID });
        }

        public static async Task<IEnumerable<T>> ObterListaPorUsuarioAsync<T>(this IDbConnection connection, long usuarioID)
        {
            string colFK_Usuario = PadronizarNome("FK_Usuario");

            string condicao = $"{colFK_Usuario} = @UsuarioID";

            return await connection.ObterListaAsync<T>(condicao, new { UsuarioID = usuarioID });
        }
        #endregion

        #region MÉTODOS AUXILIARES (HELPERS)
        public static eTipoBancoDados IdentificarBanco(this IDbConnection connection)
        {
            if (connection is SqlConnection) 
                return eTipoBancoDados.SQLServer;

            if (connection is SqliteConnection)
                return eTipoBancoDados.SQLite;

            return eTipoBancoDados.Nenhum;
        }

        private static string PadronizarNome(string valor)
        {
            // Garante que o valor não seja padronizado duas vezes caso já tenha colchetes
            if (valor.StartsWith("[") && valor.EndsWith("]")) return valor;
            return $"[{valor}]";
        }

        private static string ObterNomeChavePrimaria(Type tipo)
        {
            var propriedades = tipo.GetProperties();
            var propriedadeChave = propriedades.FirstOrDefault(p => p.GetCustomAttribute<ChavePrimaria>() != null);

            if (propriedadeChave != null)
                return propriedadeChave.Name; // Retorna o nome puro

            var fallback = propriedades.FirstOrDefault(p =>
                p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase) ||
                p.Name.Equals($"PK_{tipo.Name}", StringComparison.OrdinalIgnoreCase));

            if (fallback != null)
                return fallback.Name; // Retorna o nome puro

            throw new InvalidOperationException($"Chave primária não encontrada em {tipo.Name}");
        }

        private static string ObterNomeTabelaFormatado(Type tipo)
        {
            string nomeTabela;
            var atributoEntidade = tipo.GetCustomAttribute<EntidadeAttribute>();

            if (atributoEntidade != null && !string.IsNullOrEmpty(atributoEntidade.NomeTabela))
                nomeTabela = atributoEntidade.NomeTabela;
            else
                nomeTabela = tipo.Name;

            return PadronizarNome(nomeTabela);
        }

        // NOVO MÉTODO: Extrai as propriedades válidas para Insert e Update (Limpa muito código!)
        private static IEnumerable<PropertyInfo> ObterPropriedadesMapeaveis(Type tipo)
        {
            var nomePk = ObterNomeChavePrimaria(tipo);

            return tipo.GetProperties()
                .Where(p =>
                    !p.Name.Equals(nomePk, StringComparison.OrdinalIgnoreCase) && // Pula a PK
                    p.GetCustomAttribute<ChavePrimaria>() == null &&              // Pula atributos de PK explícitos
                    p.GetCustomAttribute<Editavel>()?.HabilitarEdicao != false    // Pula os Editavel(false)
                );
        }
        #endregion
    }
}
