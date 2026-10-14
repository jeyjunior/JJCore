using JJCore.Domain.Enums;
using System.Text;

namespace JJCore.Infra.Data.Translators
{
    /// <summary>
    /// Translates JJCore SQL identifier syntax to the selected database provider.
    /// </summary>
    public static class SqlTranslator
    {
        /// <summary>
        /// Translates bracketed identifiers, such as [Column], to the identifier quotes of the selected database provider.
        /// </summary>
        /// <remarks>
        /// SQL Server and SQLite queries are returned unchanged. For PostgreSQL and MySQL, brackets inside string literals,
        /// quoted identifiers, comments, and PostgreSQL dollar-quoted strings are preserved. A bracket that directly follows
        /// an identifier, a closing parenthesis, or another bracket is treated as an array subscript, such as ARRAY[1, 2]
        /// or items[1], and is also preserved.
        /// </remarks>
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
                DatabaseType.PostgreSQL => TranslateIdentifiers(query, '"', isMySql: false),
                DatabaseType.MySQL => TranslateIdentifiers(query, '`', isMySql: true),
                _ => throw new NotSupportedException($"Database {databaseType} is not supported.")
            };
        }

        private static string TranslateIdentifiers(string query, char quote, bool isMySql)
        {
            var builder = new StringBuilder(query.Length);
            int index = 0;

            while (index < query.Length)
            {
                if (query[index] == '[' && TryFindBracketIdentifierEnd(query, index, out int closeIndex))
                {
                    builder.Append(quote).Append(query, index + 1, closeIndex - index - 1).Append(quote);
                    index = closeIndex + 1;
                    continue;
                }

                int segmentEnd = GetSegmentEnd(query, index, isMySql);
                builder.Append(query, index, segmentEnd - index);
                index = segmentEnd;
            }

            return builder.ToString();
        }

        // Returns the end of the literal, comment, or quoted segment starting at index, or index + 1 for any other character.
        private static int GetSegmentEnd(string query, int index, bool isMySql)
        {
            char current = query[index];
            char next = index + 1 < query.Length ? query[index + 1] : '\0';

            if (current == '-' && next == '-' && (!isMySql || index + 2 >= query.Length || char.IsWhiteSpace(query[index + 2])))
            {
                return GetLineEnd(query, index);
            }

            if (current == '#' && isMySql)
            {
                return GetLineEnd(query, index);
            }

            if (current == '/' && next == '*')
            {
                return GetBlockCommentEnd(query, index, allowNesting: !isMySql);
            }

            if (current == '\'')
            {
                return GetQuotedEnd(query, index, '\'', allowBackslashEscape: isMySql || IsEscapeStringPrefix(query, index));
            }

            if (current == '"')
            {
                return GetQuotedEnd(query, index, '"', allowBackslashEscape: isMySql);
            }

            if (current == '`' && isMySql)
            {
                return GetQuotedEnd(query, index, '`', allowBackslashEscape: false);
            }

            if (current == '$' && !isMySql && TryReadDollarTag(query, index, out string tag))
            {
                int closeIndex = query.IndexOf(tag, index + tag.Length, StringComparison.Ordinal);
                return closeIndex < 0 ? query.Length : closeIndex + tag.Length;
            }

            return index + 1;
        }

        private static bool TryFindBracketIdentifierEnd(string query, int openIndex, out int closeIndex)
        {
            closeIndex = -1;

            if (openIndex > 0 && IsSubscriptPrefix(query[openIndex - 1]))
            {
                return false;
            }

            int end = query.IndexOf(']', openIndex + 1);
            bool hasNameCharacter = false;

            if (end < 0)
            {
                return false;
            }

            for (int i = openIndex + 1; i < end; i++)
            {
                char current = query[i];

                if (char.IsLetter(current) || current == '_')
                {
                    hasNameCharacter = true;
                }
                else if (!char.IsDigit(current) && current != ' ' && current != '$' && current != '#')
                {
                    return false;
                }
            }

            closeIndex = end;
            return hasNameCharacter;
        }

        private static bool IsSubscriptPrefix(char value)
        {
            return char.IsLetterOrDigit(value) || value is '_' or ')' or ']' or '[' or '"' or '`';
        }

        private static bool IsEscapeStringPrefix(string query, int quoteIndex)
        {
            return quoteIndex > 0 &&
                   (query[quoteIndex - 1] == 'E' || query[quoteIndex - 1] == 'e') &&
                   (quoteIndex == 1 || !IsIdentifierCharacter(query[quoteIndex - 2]));
        }

        private static bool TryReadDollarTag(string query, int index, out string tag)
        {
            tag = string.Empty;

            if (index > 0 && IsIdentifierCharacter(query[index - 1]))
            {
                return false;
            }

            int end = index + 1;

            if (end < query.Length && char.IsDigit(query[end]))
            {
                return false;
            }

            while (end < query.Length && (char.IsLetterOrDigit(query[end]) || query[end] == '_'))
            {
                end++;
            }

            if (end >= query.Length || query[end] != '$')
            {
                return false;
            }

            tag = query.Substring(index, end - index + 1);
            return true;
        }

        private static bool IsIdentifierCharacter(char value)
        {
            return char.IsLetterOrDigit(value) || value is '_' or '$';
        }

        private static int GetLineEnd(string query, int index)
        {
            int lineEnd = query.IndexOf('\n', index);
            return lineEnd < 0 ? query.Length : lineEnd + 1;
        }

        private static int GetBlockCommentEnd(string query, int index, bool allowNesting)
        {
            int depth = 0;
            int position = index;

            while (position < query.Length - 1)
            {
                if (query[position] == '/' && query[position + 1] == '*' && (allowNesting || depth == 0))
                {
                    depth++;
                    position += 2;
                }
                else if (query[position] == '*' && query[position + 1] == '/')
                {
                    depth--;
                    position += 2;

                    if (depth == 0)
                    {
                        return position;
                    }
                }
                else
                {
                    position++;
                }
            }

            return query.Length;
        }

        private static int GetQuotedEnd(string query, int index, char quote, bool allowBackslashEscape)
        {
            int position = index + 1;

            while (position < query.Length)
            {
                char current = query[position];

                if (allowBackslashEscape && current == '\\')
                {
                    position += 2;
                }
                else if (current == quote)
                {
                    if (position + 1 < query.Length && query[position + 1] == quote)
                    {
                        position += 2;
                    }
                    else
                    {
                        return position + 1;
                    }
                }
                else
                {
                    position++;
                }
            }

            return query.Length;
        }
    }
}
