namespace JJCore.Domain.Enums
{
    /// <summary>
    /// Identifies the database provider used by JJCore data operations.
    /// </summary>
    public enum DatabaseType
    {
        /// <summary>
        /// No supported database provider was identified.
        /// </summary>
        None = 0,

        /// <summary>
        /// SQLite database provider.
        /// </summary>
        SQLite = 1,

        /// <summary>
        /// Microsoft SQL Server database provider.
        /// </summary>
        SQLServer = 2,

        /// <summary>
        /// PostgreSQL database provider, including Supabase databases.
        /// </summary>
        PostgreSQL = 3,

        /// <summary>
        /// MySQL database provider, including compatible MariaDB databases.
        /// </summary>
        MySQL = 4
    }

    /// <summary>
    /// Defines simple text validation formats used by JJCore helper messages.
    /// </summary>
    public enum TextFormat
    {
        /// <summary>
        /// Allows letters only.
        /// </summary>
        Letters = 1,

        /// <summary>
        /// Allows letters and numbers.
        /// </summary>
        LettersAndNumbers = 2
    }
}
