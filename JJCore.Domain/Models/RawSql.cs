namespace JJCore.Domain.Models
{
    /// <summary>
    /// Wraps SQL written by the application that JJCore must send without translating bracketed identifiers.
    /// </summary>
    /// <remarks>
    /// For PostgreSQL and MySQL, JJCore converts [Column] in conditions and custom SQL to the native identifier quotes.
    /// Use RawSql when that conversion must not happen, for example for PostgreSQL array expressions such as
    /// ARRAY [name] or tags [i]. The SQL that JJCore generates itself, such as SELECT column lists, is still translated.
    /// </remarks>
    public readonly struct RawSql
    {
        /// <summary>
        /// Creates raw SQL that is sent without identifier translation.
        /// </summary>
        /// <param name="text">The SQL text, written with the native identifier quotes of the target database.</param>
        public RawSql(string text)
        {
            Text = text ?? throw new ArgumentNullException(nameof(text));
        }

        /// <summary>
        /// Gets the SQL text. It is null only for the default value of the structure.
        /// </summary>
        public string Text { get; }

        /// <inheritdoc />
        public override string ToString() => Text ?? string.Empty;
    }
}
