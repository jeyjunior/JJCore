using JJCore.Domain.Enumeradores;
using JJCore.Domain.Extensao;
using JJCore.Domain.Modelos;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Npgsql;
using System.Data;
using System.Text;

namespace JJCore.Infra.Data.Factory
{
    public static class ConnectionFactory
    {
        public static IDbConnection Criar(string connectionString, eTipoBancoDados tipoBanco)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("A string de conexão não pode ser vazia.", nameof(connectionString));
            }

            return tipoBanco switch
            {
                eTipoBancoDados.SQLServer => new SqlConnection(connectionString),
                eTipoBancoDados.SQLite => new SqliteConnection(connectionString),
                eTipoBancoDados.PostgreSQL => new NpgsqlConnection(connectionString),
                _ => throw new NotSupportedException($"O banco de dados {tipoBanco} não é suportado.")
            };
        }

        public static IDbConnection CriarDoArquivo(string caminhoArquivo = "parametros.json")
        {
            var arquivo = JsonHelper.LerArquivo<ArquivoParametrosDto>(caminhoArquivo);

            if (arquivo == null || arquivo.Conexoes.Count == 0)
            {
                throw new InvalidOperationException("O arquivo de parâmetros está vazio ou inválido.");
            }

            var conexoesAtivas = arquivo.Conexoes.Where(c => c.Ativo).ToList();

            if (conexoesAtivas.Count == 0)
            {
                throw new InvalidOperationException("Nenhuma conexão ativa foi encontrada no arquivo de parâmetros.");
            }

            if (conexoesAtivas.Count > 1)
            {
                throw new InvalidOperationException("Mais de uma conexão ativa foi encontrada no arquivo de parâmetros.");
            }

            var configAtiva = conexoesAtivas[0];
            string stringConexaoFinal = configAtiva.Conexao;

            if (configAtiva.EhStringBase64)
            {
                var base64Bytes = Convert.FromBase64String(stringConexaoFinal);
                stringConexaoFinal = Encoding.UTF8.GetString(base64Bytes);
            }

            return Criar(stringConexaoFinal, configAtiva.TipoBancoDados);
        }

        public static void GerarArquivoParametros(ArquivoParametrosDto parametros, string caminhoArquivo = "parametros.json")
        {
            if (parametros == null || parametros.Conexoes.Count == 0)
            {
                throw new ArgumentException("A lista de conexões fornecida está vazia ou nula.", nameof(parametros));
            }

            JsonHelper.SalvarArquivo(parametros, caminhoArquivo);
        }

        public static void GerarArquivoPadrao(string caminhoArquivo = "parametros.json")
        {
            var template = new ArquivoParametrosDto
            {
                Conexoes = new List<ParametroConexaoDto>
                {
                    new ParametroConexaoDto
                    {
                        NomeConexao = "Producao_SQLServer",
                        Conexao = "Server=MEU_SERVIDOR;Database=MEU_BANCO;User Id=MEU_USUARIO;Password=MINHA_SENHA;TrustServerCertificate=True;",
                        TipoBancoDados = eTipoBancoDados.SQLServer,
                        Ativo = true,
                        EhStringBase64 = false
                    },
                    new ParametroConexaoDto
                    {
                        NomeConexao = "Local_SQLite",
                        Conexao = "Data Source=banco_local.db;",
                        TipoBancoDados = eTipoBancoDados.SQLite,
                        Ativo = false,
                        EhStringBase64 = false
                    },
                    new ParametroConexaoDto
                    {
                        NomeConexao = "Supabase_PostgreSQL",
                        Conexao = "Host=MEU_HOST_SUPABASE;Port=5432;Database=postgres;Username=postgres;Password=MINHA_SENHA;SSL Mode=Require;Trust Server Certificate=true;",
                        TipoBancoDados = eTipoBancoDados.PostgreSQL,
                        Ativo = false,
                        EhStringBase64 = false
                    }
                }
            };

            JsonHelper.SalvarArquivo(template, caminhoArquivo);
        }
    }
}
