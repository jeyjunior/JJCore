using JJCore.Domain.Enums;

namespace JJCore.Infra.Data.Translators
{
    /// <summary>
    /// Translates JJCore SQL identifier syntax to the selected database provider.
    /// </summary>
    public static class SqlTranslator
    {
        /// <summary>
        /// Translates a SQL query generated with bracketed identifiers to the selected database provider.
        /// </summary>
        /// <param name="query">The SQL query to translate.</param>
        /// <param name="databaseType">The target database provider.</param>
        /// <returns>The translated SQL query.</returns>
        public static string TranslateQuery(string query, DatabaseType databaseType)
        {
            if (string.IsNullOrEmpty(query))
            {
                return query;
            }

            return databaseType switch
            {
                DatabaseType.SQLServer or DatabaseType.SQLite => query,
                DatabaseType.PostgreSQL => query.Replace("[", "\"").Replace("]", "\""),
                DatabaseType.MySQL => query.Replace("[", "`").Replace("]", "`"),
                _ => throw new NotSupportedException($"Database {databaseType} is not supported.")
            };
        }
    }
}
