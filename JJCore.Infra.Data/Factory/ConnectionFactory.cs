using JJCore.Domain.Enumeradores;
using JJCore.Domain.Extensao;
using JJCore.Domain.Modelos;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JJCore.Infra.Data.Factory
{
    public static class ConnectionFactory
    {
        public static IDbConnection Criar(string connectionString, eTipoBancoDados tipoBanco)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("A string de conexão não pode ser vazia.");

            IDbConnection connection = tipoBanco switch
            {
                eTipoBancoDados.SQLServer => new SqlConnection(connectionString),
                eTipoBancoDados.SQLite => new SqliteConnection(connectionString),
                // eTipoBancoDados.PostgreSQL => new NpgsqlConnection(connectionString),
                _ => throw new NotSupportedException($"O banco de dados {tipoBanco} não é suportado.")
            };

            return connection;
        }

        public static IDbConnection CriarDoArquivo(string caminhoArquivo = "parametros.json")
        {
            var arquivo = JsonHelper.LerArquivo<ArquivoParametrosDto>(caminhoArquivo);

            if (arquivo == null || !arquivo.Conexoes.Any())
                throw new InvalidOperationException("O arquivo de parâmetros está vazio ou inválido.");

            var configAtiva = arquivo.Conexoes.FirstOrDefault(c => c.Ativo);

            if (configAtiva == null)
                throw new InvalidOperationException("Nenhuma conexão ativa foi encontrada no arquivo de parâmetros.");

            string stringConexaoFinal = configAtiva.Conexao;

            if (configAtiva.EhStringBase64)
            {
                var base64Bytes = Convert.FromBase64String(stringConexaoFinal);
                stringConexaoFinal = Encoding.UTF8.GetString(base64Bytes);
            }

            return Criar(stringConexaoFinal, configAtiva.TipoBancoDados);
        }
    }
}
