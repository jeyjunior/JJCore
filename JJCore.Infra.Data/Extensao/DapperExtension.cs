using Dapper;
using JJCore.Domain.Atributos;
using JJCore.Domain.Enumeradores;
using JJCore.Infra.Data.Dicionario;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Npgsql;
using System.Collections.Concurrent;
using System.Data;
using System.Globalization;
using System.Reflection;

namespace JJCore.Infra.Data.Extensao
{
    public static class DapperExtension
    {
        private static readonly ConcurrentDictionary<Type, MapeamentoEntidade> Mapeamentos = new();

        public static async Task<T?> ObterAsync<T>(this IDbConnection connection, object id, IDbTransaction? transaction = null)
        {
            ValidarBancoSuportado(connection);

            var mapeamento = ObterMapeamento(typeof(T));
            string nomeTabela = PadronizarNome(mapeamento.NomeTabela);
            string colunas = ObterColunasSelect(mapeamento);
            string nomePK = PadronizarNome(mapeamento.ChavePrimaria.NomeColuna);
            string sql = $"SELECT {colunas} FROM {nomeTabela} WHERE {nomePK} = @Id";

            sql = SQLTradutor.TraduzirQuery(sql, connection.IdentificarBanco());

            return await connection.QuerySingleOrDefaultAsync<T>(sql, new { Id = id }, transaction);
        }

        public static async Task<T?> ObterAsync<T>(this IDbConnection connection, string condicao, object? parametros = null, IDbTransaction? transaction = null)
        {
            ValidarBancoSuportado(connection);

            if (string.IsNullOrWhiteSpace(condicao))
            {
                throw new ArgumentException("Para buscar sem ID, a condicao WHERE e obrigatoria.", nameof(condicao));
            }

            var mapeamento = ObterMapeamento(typeof(T));
            string nomeTabela = PadronizarNome(mapeamento.NomeTabela);
            string colunas = ObterColunasSelect(mapeamento);
            string sql = $"SELECT {colunas} FROM {nomeTabela} WHERE {condicao}";

            sql = SQLTradutor.TraduzirQuery(sql, connection.IdentificarBanco());

            return await connection.QuerySingleOrDefaultAsync<T>(sql, parametros, transaction);
        }

        public static async Task<IEnumerable<T>> ObterListaAsync<T>(this IDbConnection connection, string condicao = "", object? parametros = null, IDbTransaction? transaction = null)
        {
            ValidarBancoSuportado(connection);

            var mapeamento = ObterMapeamento(typeof(T));
            string nomeTabela = PadronizarNome(mapeamento.NomeTabela);
            string colunas = ObterColunasSelect(mapeamento);
            string sql = $"SELECT {colunas} FROM {nomeTabela}";

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
            var mapeamento = ObterMapeamento(typeof(T));

            ValidarPropriedadesMapeaveis(mapeamento.PropriedadesMapeaveis, mapeamento.Tipo);

            var colunas = mapeamento.PropriedadesMapeaveis.Select(p => PadronizarNome(p.NomeColuna));
            var paramNames = mapeamento.PropriedadesMapeaveis.Select(p => "@" + p.Propriedade.Name);
            var parametros = lista.Select(entity => CriarParametros(entity, mapeamento.PropriedadesMapeaveis)).ToList();
            string tabela = PadronizarNome(mapeamento.NomeTabela);
            string sql = $"INSERT INTO {tabela} ({string.Join(", ", colunas)}) VALUES ({string.Join(", ", paramNames)})";

            sql = SQLTradutor.TraduzirQuery(sql, tipoBanco);

            return await connection.ExecuteAsync(sql, parametros, transaction);
        }

        public static async Task<int> AdicionarAsync<T>(this IDbConnection connection, T entity, IDbTransaction? transaction = null)
        {
            object? result = await connection.AdicionarRetornandoAsync(entity, transaction);

            try
            {
                return Convert.ToInt32(result, CultureInfo.InvariantCulture);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("A chave retornada nao pode ser convertida para int. Use AdicionarRetornandoAsync para chaves de outro tipo.", ex);
            }
        }

        public static async Task<object?> AdicionarRetornandoAsync<T>(this IDbConnection connection, T entity, IDbTransaction? transaction = null)
        {
            ValidarBancoSuportado(connection);
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            var tipoBanco = connection.IdentificarBanco();
            var mapeamento = ObterMapeamento(typeof(T));

            ValidarPropriedadesMapeaveis(mapeamento.PropriedadesMapeaveis, mapeamento.Tipo);

            var colunas = mapeamento.PropriedadesMapeaveis.Select(p => PadronizarNome(p.NomeColuna));
            var paramNames = mapeamento.PropriedadesMapeaveis.Select(p => "@" + p.Propriedade.Name);
            var parametros = CriarParametros(entity, mapeamento.PropriedadesMapeaveis);
            string nomeTabela = PadronizarNome(mapeamento.NomeTabela);
            string nomePkFormatado = PadronizarNome(mapeamento.ChavePrimaria.NomeColuna);
            string cols = string.Join(", ", colunas);
            string vals = string.Join(", ", paramNames);
            string sql = tipoBanco switch
            {
                eTipoBancoDados.SQLServer => $"INSERT INTO {nomeTabela} ({cols}) VALUES ({vals}); SELECT CAST(SCOPE_IDENTITY() as int);",
                eTipoBancoDados.SQLite => $"INSERT INTO {nomeTabela} ({cols}) VALUES ({vals}); SELECT last_insert_rowid();",
                eTipoBancoDados.PostgreSQL => $"INSERT INTO {nomeTabela} ({cols}) VALUES ({vals}) RETURNING {nomePkFormatado};",
                _ => throw new NotSupportedException($"O banco de dados {tipoBanco} nao e suportado.")
            };

            sql = SQLTradutor.TraduzirQuery(sql, tipoBanco);

            object? result = await connection.ExecuteScalarAsync<object>(sql, parametros, transaction);

            if (result == null || result == DBNull.Value)
            {
                throw new InvalidOperationException("Nenhuma linha foi inserida no banco de dados.");
            }

            return result;
        }

        public static async Task<int> AtualizarAsync<T>(this IDbConnection connection, T entity, IDbTransaction? transaction = null)
        {
            ValidarBancoSuportado(connection);
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            var tipoBanco = connection.IdentificarBanco();
            var mapeamento = ObterMapeamento(typeof(T));

            ValidarPropriedadesMapeaveis(mapeamento.PropriedadesMapeaveis, mapeamento.Tipo);

            var valorPk = mapeamento.ChavePrimaria.Propriedade.GetValue(entity);
            if (valorPk == null)
            {
                throw new InvalidOperationException($"A chave primaria {mapeamento.ChavePrimaria.Propriedade.Name} nao foi preenchida.");
            }

            var parametros = CriarParametros(entity, mapeamento.PropriedadesMapeaveis);
            var colunasUpdate = mapeamento.PropriedadesMapeaveis
                .Select(p => $"{PadronizarNome(p.NomeColuna)} = @{p.Propriedade.Name}");

            parametros.Add("Id", valorPk);

            string nomeTabela = PadronizarNome(mapeamento.NomeTabela);
            string nomePkFormatado = PadronizarNome(mapeamento.ChavePrimaria.NomeColuna);
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
            var mapeamento = ObterMapeamento(typeof(T));
            string nomeTabela = PadronizarNome(mapeamento.NomeTabela);
            string nomePkFormatado = PadronizarNome(mapeamento.ChavePrimaria.NomeColuna);
            string sql = $"DELETE FROM {nomeTabela} WHERE {nomePkFormatado} = @Id";

            sql = SQLTradutor.TraduzirQuery(sql, tipoBanco);

            int linhas = await connection.ExecuteAsync(sql, new { Id = id }, transaction);

            if (linhas == 0)
            {
                throw new InvalidOperationException("O registro a ser excluido nao existe.");
            }

            return linhas;
        }

        public static async Task<int> DeletarTudoAsync<T>(this IDbConnection connection, IDbTransaction? transaction = null)
        {
            ValidarBancoSuportado(connection);

            var tipoBanco = connection.IdentificarBanco();
            var mapeamento = ObterMapeamento(typeof(T));
            string nomeTabela = PadronizarNome(mapeamento.NomeTabela);
            string sql = $"DELETE FROM {nomeTabela}";

            sql = SQLTradutor.TraduzirQuery(sql, tipoBanco);

            return await connection.ExecuteAsync(sql, transaction: transaction);
        }

        public static async Task<int> ExecutarQueryAsync(this IDbConnection connection, string query, object? parametros = null, IDbTransaction? transaction = null)
        {
            ValidarBancoSuportado(connection);

            if (string.IsNullOrWhiteSpace(query))
            {
                throw new ArgumentException("A query fornecida nao pode ser nula ou vazia.", nameof(query));
            }

            query = SQLTradutor.TraduzirQuery(query, connection.IdentificarBanco());

            return await connection.ExecuteAsync(query, parametros, transaction);
        }

        public static async Task<long> ObterProximoCodigoAsync<T>(this IDbConnection connection, long usuarioID, long valorInicial = 1, IDbTransaction? transaction = null)
        {
            ValidarBancoSuportado(connection);

            var tipoBanco = connection.IdentificarBanco();
            var mapeamento = ObterMapeamento(typeof(T));
            var propriedadeCodigo = ObterPropriedadeCodigoUsuario(mapeamento);
            var propriedadeUsuario = ObterPropriedadeUsuario(mapeamento);
            string tabela = PadronizarNome(mapeamento.NomeTabela);
            string colCodigo = PadronizarNome(propriedadeCodigo.NomeColuna);
            string colFK = PadronizarNome(propriedadeUsuario.NomeColuna);
            long valorBase = valorInicial - 1;
            string sql = $"SELECT COALESCE(MAX({colCodigo}), @ValorBase) + 1 FROM {tabela} WHERE {colFK} = @UsuarioID";

            sql = SQLTradutor.TraduzirQuery(sql, tipoBanco);

            var result = await connection.ExecuteScalarAsync<object>(sql, new { UsuarioID = usuarioID, ValorBase = valorBase }, transaction);

            return Convert.ToInt64(result, CultureInfo.InvariantCulture);
        }

        public static async Task<T?> ObterPorCodigoAsync<T>(this IDbConnection connection, long codigo, long usuarioID, IDbTransaction? transaction = null)
        {
            var mapeamento = ObterMapeamento(typeof(T));
            var propriedadeCodigo = ObterPropriedadeCodigoUsuario(mapeamento);
            var propriedadeUsuario = ObterPropriedadeUsuario(mapeamento);
            string colCodigo = PadronizarNome(propriedadeCodigo.NomeColuna);
            string colFKUsuario = PadronizarNome(propriedadeUsuario.NomeColuna);
            string condicao = $"{colCodigo} = @Codigo AND {colFKUsuario} = @UsuarioID";

            return await connection.ObterAsync<T>(condicao, new { Codigo = codigo, UsuarioID = usuarioID }, transaction);
        }

        public static async Task<IEnumerable<T>> ObterListaPorUsuarioAsync<T>(this IDbConnection connection, long usuarioID, IDbTransaction? transaction = null)
        {
            var mapeamento = ObterMapeamento(typeof(T));
            var propriedadeUsuario = ObterPropriedadeUsuario(mapeamento);
            string colFKUsuario = PadronizarNome(propriedadeUsuario.NomeColuna);
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
                throw new NotSupportedException($"O tipo de conexao {connection.GetType().Name} nao e suportado.");
            }
        }

        private static DynamicParameters CriarParametros<T>(T entity, IReadOnlyList<PropriedadeMapeada> propriedades)
        {
            var parametros = new DynamicParameters();

            foreach (var propriedade in propriedades)
            {
                parametros.Add(propriedade.Propriedade.Name, ObterValorTratado(entity, propriedade.Propriedade));
            }

            return parametros;
        }

        private static object? ObterValorTratado<T>(T entity, PropertyInfo propriedade)
        {
            var valor = propriedade.GetValue(entity);

            if (propriedade.GetCustomAttribute<Obrigatorio>() != null && valor == null)
            {
                throw new InvalidOperationException($"A propriedade {propriedade.Name} e obrigatoria e nao foi preenchida.");
            }

            if (propriedade.PropertyType == typeof(DateTime) && (valor == null || (DateTime)valor == DateTime.MinValue))
            {
                return DateTime.UtcNow;
            }

            return valor;
        }

        private static string ObterColunasSelect(MapeamentoEntidade mapeamento)
        {
            return string.Join(", ", mapeamento.PropriedadesLeitura
                .Select(p => $"{PadronizarNome(p.NomeColuna)} AS {PadronizarNome(p.Propriedade.Name)}"));
        }

        private static string PadronizarNome(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                throw new ArgumentException("O nome informado nao pode ser vazio.", nameof(valor));
            }

            var partes = valor.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim());
            return string.Join(".", partes.Select(PadronizarParteNome));
        }

        private static string PadronizarParteNome(string valor)
        {
            var nome = valor.Trim('[', ']');

            if (nome.Length == 0 || nome.Any(c => !char.IsLetterOrDigit(c) && c != '_'))
            {
                throw new ArgumentException($"O nome '{valor}' contem caracteres invalidos.");
            }

            return $"[{nome}]";
        }

        private static MapeamentoEntidade ObterMapeamento(Type tipo)
        {
            return Mapeamentos.GetOrAdd(tipo, CriarMapeamento);
        }

        private static MapeamentoEntidade CriarMapeamento(Type tipo)
        {
            var propriedades = tipo.GetProperties()
                .Where(p => p.GetCustomAttribute<NaoMapear>() == null)
                .Select(p => new PropriedadeMapeada(p, ObterNomeColuna(p)))
                .ToList();

            var chavePrimaria = propriedades.FirstOrDefault(p => p.Propriedade.GetCustomAttribute<ChavePrimaria>() != null);

            if (chavePrimaria == null)
            {
                chavePrimaria = propriedades.FirstOrDefault(p =>
                    p.Propriedade.Name.Equals("Id", StringComparison.OrdinalIgnoreCase) ||
                    p.Propriedade.Name.Equals($"PK_{tipo.Name}", StringComparison.OrdinalIgnoreCase));
            }

            if (chavePrimaria == null)
            {
                throw new InvalidOperationException($"Chave primaria nao encontrada em {tipo.Name}.");
            }

            var atributoEntidade = tipo.GetCustomAttribute<EntidadeAttribute>();
            string? nomeTabela = atributoEntidade?.NomeTabela;

            if (string.IsNullOrWhiteSpace(nomeTabela))
            {
                nomeTabela = tipo.Name;
            }

            var propriedadesMapeaveis = propriedades
                .Where(p =>
                    p.Propriedade != chavePrimaria.Propriedade &&
                    p.Propriedade.GetCustomAttribute<ChavePrimaria>() == null &&
                    p.Propriedade.GetCustomAttribute<Editavel>()?.HabilitarEdicao != false)
                .ToList();

            return new MapeamentoEntidade(tipo, nomeTabela!, chavePrimaria, propriedades, propriedadesMapeaveis);
        }

        private static string ObterNomeColuna(PropertyInfo propriedade)
        {
            return propriedade.GetCustomAttribute<Coluna>()?.Nome ?? propriedade.Name;
        }

        private static PropriedadeMapeada ObterPropriedadeCodigoUsuario(MapeamentoEntidade mapeamento)
        {
            return mapeamento.PropriedadesLeitura.FirstOrDefault(p => p.Propriedade.GetCustomAttribute<CodigoUsuario>() != null)
                ?? mapeamento.PropriedadesLeitura.FirstOrDefault(p => p.Propriedade.Name.Equals("Codigo", StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException($"Propriedade Codigo nao encontrada em {mapeamento.Tipo.Name}.");
        }

        private static PropriedadeMapeada ObterPropriedadeUsuario(MapeamentoEntidade mapeamento)
        {
            return mapeamento.PropriedadesLeitura.FirstOrDefault(p => p.Propriedade.Name.Equals("FK_Usuario", StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException($"Propriedade FK_Usuario nao encontrada em {mapeamento.Tipo.Name}.");
        }

        private static void ValidarPropriedadesMapeaveis(IReadOnlyCollection<PropriedadeMapeada> propriedades, Type tipo)
        {
            if (propriedades.Count == 0)
            {
                throw new InvalidOperationException($"Nenhuma propriedade mapeavel foi encontrada em {tipo.Name}.");
            }
        }

        private sealed class MapeamentoEntidade
        {
            public MapeamentoEntidade(
                Type tipo,
                string nomeTabela,
                PropriedadeMapeada chavePrimaria,
                IReadOnlyList<PropriedadeMapeada> propriedadesLeitura,
                IReadOnlyList<PropriedadeMapeada> propriedadesMapeaveis)
            {
                Tipo = tipo;
                NomeTabela = nomeTabela;
                ChavePrimaria = chavePrimaria;
                PropriedadesLeitura = propriedadesLeitura;
                PropriedadesMapeaveis = propriedadesMapeaveis;
            }

            public Type Tipo { get; }
            public string NomeTabela { get; }
            public PropriedadeMapeada ChavePrimaria { get; }
            public IReadOnlyList<PropriedadeMapeada> PropriedadesLeitura { get; }
            public IReadOnlyList<PropriedadeMapeada> PropriedadesMapeaveis { get; }
        }

        private sealed class PropriedadeMapeada
        {
            public PropriedadeMapeada(PropertyInfo propriedade, string nomeColuna)
            {
                Propriedade = propriedade;
                NomeColuna = nomeColuna;
            }

            public PropertyInfo Propriedade { get; }
            public string NomeColuna { get; }
        }
    }
}
