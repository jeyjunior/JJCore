using JJCore.Domain.Enums;

namespace JJCore.Infra.Data.Translators
{
    public static class SqlTranslator
    {
        public static string TranslateQuery(string query, DatabaseType databaseType)
        {
            if (string.IsNullOrEmpty(query))
            {
                return query;
            }

            return databaseType switch
            {
                DatabaseType.SQLServer => query,
                DatabaseType.SQLite => query.Replace("[", "").Replace("]", ""),
                DatabaseType.PostgreSQL => query.Replace("[", "\"").Replace("]", "\""),
                _ => throw new NotSupportedException($"Database {databaseType} is not supported.")
            };
        }
    }
}
