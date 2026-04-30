using JJCore.Domain.Enumeradores;

namespace JJCore.Infra.Data.Dicionario
{
    public static class SQLTradutor
    {
        public static string TraduzirQuery(string query, eTipoBancoDados tipoBanco)
        {
            if (string.IsNullOrEmpty(query))
            {
                return query;
            }

            return tipoBanco switch
            {
                eTipoBancoDados.SQLServer => query,
                eTipoBancoDados.SQLite => query.Replace("[", "").Replace("]", ""),
                _ => throw new NotSupportedException($"O banco de dados {tipoBanco} não é suportado.")
            };
        }
    }
}
