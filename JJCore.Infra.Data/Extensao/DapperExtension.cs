using Dapper;
using JJCore.Domain.Atributos;
using JJCore.Domain.Enumeradores;
using JJCore.Infra.Data.Dicionario;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Npgsql;
using System.Data;
using System.Reflection;

namespace JJCore.Infra.Data.Extensao
{
    public static class DapperExtension
    {
        public static async Task<T?> ObterAsync<T>(this IDbConnection connection, object id, IDbTransaction? transaction = null)
        {
            ValidarBancoSuportado(connection);

            var tipoEntidade = typeof(T);
            string nomeTabela = ObterNomeTabelaFormatado(tipoEntidade);
            string nomePK = PadronizarNome(ObterNomeChavePrimaria(tipoEntidade));
            string sql = $"SELECT {nomeTabela}.* FROM {nomeTabela} WHERE {nomePK} = @Id";

            sql = SQLTradutor.TraduzirQuery(sql, connection.IdentificarBanco());

            return await connection.QuerySingleOrDefaultAsync<T>(sql, new { Id = id }, transaction);
        }

        public static async Task<T?> ObterAsync<T>(this IDbConnection connection, string condicao, object? parametros = null, IDbTransaction? transaction = null)
        {
            ValidarBancoSuportado(connection);

            if (string.IsNullOrWhiteSpace(condicao))
            {
                throw new ArgumentException("Para buscar sem ID, a condição WHERE é obrigatória.", nameof(condicao));
            }

            string nomeTabela = ObterNomeTabelaFormatado(typeof(T));
            string sql = $"SELECT {nomeTabela}.* FROM {nomeTabela} WHERE {condicao}";

            sql = SQLTradutor.TraduzirQuery(sql, connection.IdentificarBanco());

            return await connection.QuerySingleOrDefaultAsync<T>(sql, parametros, transaction);
        }

        public static async Task<IEnumerable<T>> ObterListaAsync<T>(this IDbConnection connection, string condicao = "", object? parametros = null, IDbTransaction? transaction = null)
        {
            ValidarBancoSuportado(connection);

            string nomeTabela = ObterNomeTabelaFormatado(typeof(T));
            string sql = $"SELECT {nomeTabela}.* FROM {nomeTabela}";

            if (!string.IsNullOrWhiteSpace(condicao))
            {
                sql += $" WHERE {condicao}";
            }

            sql = SQLTradutor.TraduzirQuery(sql, connection.IdentificarBanco());

            return await connection.QueryAsync<T>(sql, parametros, transaction);
        }

        public static async Task<int> AdicionarEmLoteAsync<T>(this IDbConnection connection, IEnumerable<T> entidades, IDbTransaction? transaction = null)
        {
            ValidarBancoSuportado(connection);

            var lista = entidades?.ToList();
            if (lista == null || lista.Count == 0)
            {
                return 0;
            }

            var tipoBanco = connection.IdentificarBanco();
            var entidade = typeof(T);
            string tabela = ObterNomeTabelaFormatado(entidade);
            var propriedades = ObterPropriedadesMapeaveis(entidade).ToList();

            ValidarPropriedadesMapeaveis(propriedades, entidade);

            var colunas = propriedades.Select(p => PadronizarNome(p.Name));
            var paramNames = propriedades.Select(p => "@" + p.Name);
            string sql = $"INSERT INTO {tabela} ({string.Join(", ", colunas)}) VALUES ({string.Join(", ", paramNames)})";

            sql = SQLTradutor.TraduzirQuery(sql, tipoBanco);

            return await connection.ExecuteAsync(sql, lista, transaction);
        }

        public static async Task<int> AdicionarAsync<T>(this IDbConnection connection, T entity, IDbTransaction? transaction = null)
        {
            ValidarBancoSuportado(connection);
            if (entity == null) 
                throw new ArgumentNullException(nameof(entity));

            var tipoBanco = connection.IdentificarBanco();
            var entidade = typeof(T);
            string nomeTabela = ObterNomeTabelaFormatado(entidade);
            string nomePkFormatado = PadronizarNome(ObterNomeChavePrimaria(entidade));
            var propriedades = ObterPropriedadesMapeaveis(entidade).ToList();

            ValidarPropriedadesMapeaveis(propriedades, entidade);

            var colunas = new List<string>();
            var paramNames = new List<string>();
            var parametros = new DynamicParameters();

            foreach (var propriedade in propriedades)
            {
                var valor = ObterValorTratado(entity, propriedade);

                colunas.Add(PadronizarNome(propriedade.Name));
                paramNames.Add("@" + propriedade.Name);
                parametros.Add(propriedade.Name, valor);
            }

            string cols = string.Join(", ", colunas);
            string vals = string.Join(", ", paramNames);
            string sql = tipoBanco switch
            {
                eTipoBancoDados.SQLServer => $"INSERT INTO {nomeTabela} ({cols}) VALUES ({vals}); SELECT CAST(SCOPE_IDENTITY() as int);",
                eTipoBancoDados.SQLite => $"INSERT INTO {nomeTabela} ({cols}) VALUES ({vals}); SELECT last_insert_rowid();",
                eTipoBancoDados.PostgreSQL => $"INSERT INTO {nomeTabela} ({cols}) VALUES ({vals}) RETURNING {nomePkFormatado};",
                _ => throw new NotSupportedException($"O banco de dados {tipoBanco} não é suportado.")
            };

            sql = SQLTradutor.TraduzirQuery(sql, tipoBanco);

            var result = await connection.ExecuteScalarAsync<int>(sql, parametros, transaction);

            if (result == 0)
            {
                throw new InvalidOperationException("Nenhuma linha foi inserida no banco de dados.");
            }

            return result;
        }

        public static async Task<int> AtualizarAsync<T>(this IDbConnection connection, T entity, IDbTransaction? transaction = null)
        {
            ValidarBancoSuportado(connection);
            if (entity == null) 
                throw new ArgumentNullException(nameof(entity));

            var tipoBanco = connection.IdentificarBanco();
            var entidade = typeof(T);
            string nomeTabela = ObterNomeTabelaFormatado(entidade);
            string nomePkSemFormatacao = ObterNomeChavePrimaria(entidade);
            string nomePkFormatado = PadronizarNome(nomePkSemFormatacao);
            var propriedades = ObterPropriedadesMapeaveis(entidade).ToList();

            ValidarPropriedadesMapeaveis(propriedades, entidade);

            var parametros = new DynamicParameters();
            var propPk = entidade.GetProperty(nomePkSemFormatacao);
            var valorPk = propPk?.GetValue(entity);

            if (valorPk == null)
            {
                throw new InvalidOperationException($"A chave primária {nomePkSemFormatacao} não foi preenchida.");
            }

            var colunasUpdate = new List<string>();

            foreach (var propriedade in propriedades)
            {
                var valor = ObterValorTratado(entity, propriedade);
                colunasUpdate.Add($"{PadronizarNome(propriedade.Name)} = @{propriedade.Name}");
                parametros.Add(propriedade.Name, valor);
            }

            parametros.Add("Id", valorPk);

            string sql = $"UPDATE {nomeTabela} SET {string.Join(", ", colunasUpdate)} WHERE {nomePkFormatado} = @Id";
            sql = SQLTradutor.TraduzirQuery(sql, tipoBanco);

            var rowsAffected = await connection.ExecuteAsync(sql, parametros, transaction);

            if (rowsAffected == 0)
            {
                throw new InvalidOperationException("Nenhuma linha foi atualizada. Verifique se o ID existe.");
            }

            return rowsAffected;
        }

        public static async Task<int> DeletarAsync<T>(this IDbConnection connection, object id, IDbTransaction? transaction = null)
        {
            ValidarBancoSuportado(connection);

            var tipoBanco = connection.IdentificarBanco();
            var entidade = typeof(T);
            string nomeTabela = ObterNomeTabelaFormatado(entidade);
            string nomePkFormatado = PadronizarNome(ObterNomeChavePrimaria(entidade));
            string sql = $"DELETE FROM {nomeTabela} WHERE {nomePkFormatado} = @Id";

            sql = SQLTradutor.TraduzirQuery(sql, tipoBanco);

            int linhas = await connection.ExecuteAsync(sql, new { Id = id }, transaction);

            if (linhas == 0)
            {
                throw new InvalidOperationException("O registro a ser excluído não existe.");
            }

            return linhas;
        }

        public static async Task<int> DeletarTudoAsync<T>(this IDbConnection connection, IDbTransaction? transaction = null)
        {
            ValidarBancoSuportado(connection);

            var tipoBanco = connection.IdentificarBanco();
            string nomeTabela = ObterNomeTabelaFormatado(typeof(T));
            string sql = $"DELETE FROM {nomeTabela}";

            sql = SQLTradutor.TraduzirQuery(sql, tipoBanco);

            return await connection.ExecuteAsync(sql, transaction: transaction);
        }

        public static async Task<int> ExecutarQueryAsync(this IDbConnection connection, string query, object? parametros = null, IDbTransaction? transaction = null)
        {
            ValidarBancoSuportado(connection);

            if (string.IsNullOrWhiteSpace(query))
            {
                throw new ArgumentException("A query fornecida não pode ser nula ou vazia.", nameof(query));
            }

            query = SQLTradutor.TraduzirQuery(query, connection.IdentificarBanco());

            return await connection.ExecuteAsync(query, parametros, transaction);
        }

        public static async Task<long> ObterProximoCodigoAsync<T>(this IDbConnection connection, long usuarioID, long valorInicial = 1, IDbTransaction? transaction = null)
        {
            ValidarBancoSuportado(connection);

            var tipoBanco = connection.IdentificarBanco();
            string tabela = ObterNomeTabelaFormatado(typeof(T));
            string colCodigo = PadronizarNome("Codigo");
            string colFK = PadronizarNome("FK_Usuario");
            long valorBase = valorInicial - 1;
            string sql = $"SELECT COALESCE(MAX({colCodigo}), @ValorBase) + 1 FROM {tabela} WHERE {colFK} = @UsuarioID";

            sql = SQLTradutor.TraduzirQuery(sql, tipoBanco);

            var result = await connection.ExecuteScalarAsync<object>(sql, new { UsuarioID = usuarioID, ValorBase = valorBase }, transaction);

            return Convert.ToInt64(result);
        }

        public static async Task<T?> ObterPorCodigoAsync<T>(this IDbConnection connection, long codigo, long usuarioID, IDbTransaction? transaction = null)
        {
            string colCodigo = PadronizarNome("Codigo");
            string colFKUsuario = PadronizarNome("FK_Usuario");
            string condicao = $"{colCodigo} = @Codigo AND {colFKUsuario} = @UsuarioID";

            return await connection.ObterAsync<T>(condicao, new { Codigo = codigo, UsuarioID = usuarioID }, transaction);
        }

        public static async Task<IEnumerable<T>> ObterListaPorUsuarioAsync<T>(this IDbConnection connection, long usuarioID, IDbTransaction? transaction = null)
        {
            string colFKUsuario = PadronizarNome("FK_Usuario");
            string condicao = $"{colFKUsuario} = @UsuarioID";

            return await connection.ObterListaAsync<T>(condicao, new { UsuarioID = usuarioID }, transaction);
        }

        public static eTipoBancoDados IdentificarBanco(this IDbConnection connection)
        {
            return connection switch
            {
                SqlConnection => eTipoBancoDados.SQLServer,
                SqliteConnection => eTipoBancoDados.SQLite,
                NpgsqlConnection => eTipoBancoDados.PostgreSQL,
                _ => eTipoBancoDados.Nenhum
            };
        }

        private static void ValidarBancoSuportado(IDbConnection connection)
        {
            if (connection.IdentificarBanco() == eTipoBancoDados.Nenhum)
            {
                throw new NotSupportedException($"O tipo de conexão {connection.GetType().Name} não é suportado.");
            }
        }

        private static object? ObterValorTratado<T>(T entity, PropertyInfo propriedade)
        {
            var valor = propriedade.GetValue(entity);

            if (propriedade.GetCustomAttribute<Obrigatorio>() != null && valor == null)
            {
                throw new InvalidOperationException($"A propriedade {propriedade.Name} é obrigatória e não foi preenchida.");
            }

            if (propriedade.PropertyType == typeof(DateTime) && (valor == null || (DateTime)valor == DateTime.MinValue))
            {
                return DateTime.UtcNow;
            }

            return valor;
        }

        private static string PadronizarNome(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                throw new ArgumentException("O nome informado não pode ser vazio.", nameof(valor));
            }

            var partes = valor.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim());
            return string.Join(".", partes.Select(PadronizarParteNome));
        }

        private static string PadronizarParteNome(string valor)
        {
            var nome = valor.Trim('[', ']');

            if (nome.Length == 0 || nome.Any(c => !char.IsLetterOrDigit(c) && c != '_'))
            {
                throw new ArgumentException($"O nome '{valor}' contém caracteres inválidos.");
            }

            return $"[{nome}]";
        }

        private static string ObterNomeChavePrimaria(Type tipo)
        {
            var propriedades = tipo.GetProperties();
            var propriedadeChave = propriedades.FirstOrDefault(p => p.GetCustomAttribute<ChavePrimaria>() != null);

            if (propriedadeChave != null)
            {
                return propriedadeChave.Name;
            }

            var fallback = propriedades.FirstOrDefault(p =>
                p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase) ||
                p.Name.Equals($"PK_{tipo.Name}", StringComparison.OrdinalIgnoreCase));

            if (fallback != null)
            {
                return fallback.Name;
            }

            throw new InvalidOperationException($"Chave primária não encontrada em {tipo.Name}.");
        }

        private static string ObterNomeTabelaFormatado(Type tipo)
        {
            var atributoEntidade = tipo.GetCustomAttribute<EntidadeAttribute>();
            string? nomeTabela = atributoEntidade?.NomeTabela;

            if (string.IsNullOrWhiteSpace(nomeTabela))
            {
                nomeTabela = tipo.Name;
            }

            return PadronizarNome(nomeTabela!);
        }

        private static IEnumerable<PropertyInfo> ObterPropriedadesMapeaveis(Type tipo)
        {
            var nomePk = ObterNomeChavePrimaria(tipo);

            return tipo.GetProperties()
                .Where(p =>
                    !p.Name.Equals(nomePk, StringComparison.OrdinalIgnoreCase) &&
                    p.GetCustomAttribute<ChavePrimaria>() == null &&
                    p.GetCustomAttribute<Editavel>()?.HabilitarEdicao != false);
        }

        private static void ValidarPropriedadesMapeaveis(IReadOnlyCollection<PropertyInfo> propriedades, Type tipo)
        {
            if (propriedades.Count == 0)
            {
                throw new InvalidOperationException($"Nenhuma propriedade mapeável foi encontrada em {tipo.Name}.");
            }
        }
    }
}
