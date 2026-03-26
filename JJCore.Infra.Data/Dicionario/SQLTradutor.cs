using JJCore.Domain.Enumeradores;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JJCore.Infra.Data.Dicionario
{
    public static class SQLTradutor
    {
        public static string TraduzirQuery(string query, eTipoBancoDados tipoBanco)
        {
            if (string.IsNullOrEmpty(query)) return query;

            switch (tipoBanco)
            {
                case eTipoBancoDados.PostgreSQL:
                    return query.Replace("[", "\"").Replace("]", "\"");

                case eTipoBancoDados.SQLServer:
                    return query;

                default:
                    return query.Replace("[", "").Replace("]", "");
            }
        }
    }
}
