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
        /// quoted identifiers, comments, and PostgreSQL dollar-quoted strings are preserved. In PostgreSQL, these brackets
        /// are treated as array syntax and also preserved: a bracket that directly follows a name, a closing parenthesis, or
        /// another bracket, such as items[1] or ARRAY[[1, 2]]; a bracket after the ARRAY keyword, with or without spaces,
        /// such as ARRAY [name]; and an element of an array whose first element is a nested array, such as
        /// ARRAY[[a], [b]]. A bracket that directly follows a reserved keyword, as in WHERE[Id], is an identifier. A bracket
        /// after a space and a name, such as tags [i], is an identifier, because it cannot be told apart from an alias,
        /// such as FROM Customers [c]; use RawSql for such subscripts. MySQL has no arrays, so brackets are translated after
        /// any character, as in WHERE[Id], and inside executable comments, such as /*!80000 ... */.
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

        // PostgreSQL reserved keywords, which cannot be column names without quotes. ARRAY is left out because a bracket
        // after it starts an array constructor.
        private static readonly HashSet<string> PostgreSqlReservedKeywords = new(StringComparer.OrdinalIgnoreCase)
        {
            "ALL", "ANALYSE", "ANALYZE", "AND", "ANY", "AS", "ASC", "ASYMMETRIC", "AUTHORIZATION", "BINARY", "BOTH",
            "CASE", "CAST", "CHECK", "COLLATE", "COLLATION", "COLUMN", "CONCURRENTLY", "CONSTRAINT", "CREATE", "CROSS",
            "CURRENT_CATALOG", "CURRENT_DATE", "CURRENT_ROLE", "CURRENT_SCHEMA", "CURRENT_TIME", "CURRENT_TIMESTAMP",
            "CURRENT_USER", "DEFAULT", "DEFERRABLE", "DESC", "DISTINCT", "DO", "ELSE", "END", "EXCEPT", "FALSE", "FETCH",
            "FOR", "FOREIGN", "FREEZE", "FROM", "FULL", "GRANT", "GROUP", "HAVING", "ILIKE", "IN", "INITIALLY", "INNER",
            "INTERSECT", "INTO", "IS", "ISNULL", "JOIN", "LATERAL", "LEADING", "LEFT", "LIKE", "LIMIT", "LOCALTIME",
            "LOCALTIMESTAMP", "NATURAL", "NOT", "NOTNULL", "NULL", "OFFSET", "ON", "ONLY", "OR", "ORDER", "OUTER",
            "OVERLAPS", "PLACING", "PRIMARY", "REFERENCES", "RETURNING", "RIGHT", "SELECT", "SESSION_USER", "SIMILAR",
            "SOME", "SYMMETRIC", "SYSTEM_USER", "TABLE", "TABLESAMPLE", "THEN", "TO", "TRAILING", "TRUE", "UNION",
            "UNIQUE", "USER", "USING", "VARIADIC", "VERBOSE", "WHEN", "WHERE", "WINDOW", "WITH"
        };

        private static string TranslateIdentifiers(string query, char quote, bool isMySql)
        {
            var builder = new StringBuilder(query.Length);
            var openGroups = new Stack<BracketGroup>();
            int index = 0;

            while (index < query.Length)
            {
                if (query[index] == '[' && TryFindBracketIdentifierEnd(query, index, isMySql, openGroups, out int closeIndex))
                {
                    builder.Append(quote).Append(query, index + 1, closeIndex - index - 1).Append(quote);
                    index = closeIndex + 1;
                    continue;
                }

                if (!isMySql)
                {
                    TrackGroup(query, index, openGroups);
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
                // MySQL executes the content of /*! ... */ and /*!80000 ... */, so only the opening marker is skipped.
                if (isMySql && index + 2 < query.Length && query[index + 2] == '!')
                {
                    int markerEnd = index + 3;

                    while (markerEnd < query.Length && char.IsDigit(query[markerEnd]))
                    {
                        markerEnd++;
                    }

                    return markerEnd;
                }

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

        private static bool TryFindBracketIdentifierEnd(string query, int openIndex, bool isMySql, Stack<BracketGroup> openGroups, out int closeIndex)
        {
            closeIndex = -1;

            // Array syntax exists only in PostgreSQL; in MySQL a bracket outside literals is never valid syntax.
            if (!isMySql && IsPostgreSqlArrayBracket(query, openIndex, openGroups))
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

        // Tracks the parentheses and the brackets kept as array syntax, so that a comma can be matched to its group.
        private static void TrackGroup(string query, int index, Stack<BracketGroup> openGroups)
        {
            switch (query[index])
            {
                case '[':
                    bool startsNestedArray = index + 1 < query.Length && query[index + 1] == '[';
                    openGroups.Push(startsNestedArray ? BracketGroup.NestedArray : BracketGroup.Array);
                    break;
                case '(':
                    openGroups.Push(BracketGroup.Parenthesis);
                    break;
                case ']':
                    if (openGroups.Count > 0 && openGroups.Peek() != BracketGroup.Parenthesis)
                    {
                        openGroups.Pop();
                    }

                    break;
                case ')':
                    if (openGroups.Count > 0 && openGroups.Peek() == BracketGroup.Parenthesis)
                    {
                        openGroups.Pop();
                    }

                    break;
            }
        }

        private static bool IsPostgreSqlArrayBracket(string query, int openIndex, Stack<BracketGroup> openGroups)
        {
            if (openIndex == 0)
            {
                return false;
            }

            char previous = query[openIndex - 1];

            if (previous is ')' or ']' or '[' or '"' or '`')
            {
                return true;
            }

            if (char.IsLetterOrDigit(previous) || previous == '_')
            {
                // A name directly followed by a bracket is a subscript, such as tags[1]; a reserved keyword is not, as in WHERE[Id].
                string word = GetWordBefore(query, openIndex);
                return word.Length == 0 || !PostgreSqlReservedKeywords.Contains(word);
            }

            int previousIndex = openIndex - 1;

            while (previousIndex >= 0 && char.IsWhiteSpace(query[previousIndex]))
            {
                previousIndex--;
            }

            if (previousIndex < 0)
            {
                return false;
            }

            if (query[previousIndex] == ',')
            {
                // An element of a nested array, such as [b] in ARRAY[[a], [b]].
                return openGroups.Count > 0 && openGroups.Peek() == BracketGroup.NestedArray;
            }

            // After spaces, only the ARRAY keyword makes the bracket an array; a name could be followed by an alias.
            return previousIndex < openIndex - 1 &&
                   string.Equals(GetWordBefore(query, previousIndex + 1), "ARRAY", StringComparison.OrdinalIgnoreCase);
        }

        // Returns the unqualified word that ends right before index, or an empty string when there is none.
        private static string GetWordBefore(string query, int index)
        {
            int start = index;

            while (start > 0 && IsIdentifierCharacter(query[start - 1]))
            {
                start--;
            }

            if (start == index || char.IsDigit(query[start]) || (start > 0 && query[start - 1] is '.' or '"'))
            {
                return string.Empty;
            }

            return query.Substring(start, index - start);
        }

        private enum BracketGroup
        {
            Parenthesis,
            Array,
            NestedArray
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
