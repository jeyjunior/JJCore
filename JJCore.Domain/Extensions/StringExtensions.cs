using System.Text;
using System.Text.RegularExpressions;

namespace JJCore.Domain.Extensions
{
    /// <summary>
    /// Provides reusable string helper methods.
    /// </summary>
    public static class StringExtensions
    {
        private static readonly Regex SafeNameRegex = new(@"^[\p{L}0-9\s\-\']+$", RegexOptions.Compiled);

        /// <summary>
        /// Validates whether a text is a safe name using length and character rules.
        /// </summary>
        /// <param name="text">The text to validate.</param>
        /// <param name="minLength">The minimum allowed length.</param>
        /// <param name="maxLength">The maximum allowed length.</param>
        /// <returns>True when the text is valid; otherwise, false.</returns>
        public static bool IsValidName(this string? text, int minLength = 1, int maxLength = 100)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            string validatedText = text!;

            if (validatedText.Length < minLength || validatedText.Length > maxLength)
            {
                return false;
            }

            if (!SafeNameRegex.IsMatch(validatedText))
            {
                return false;
            }

            string upper = validatedText.ToUpperInvariant();
            return !upper.Contains("DROP TABLE") &&
                   !upper.Contains("DELETE FROM") &&
                   !upper.Contains("<SCRIPT>");
        }

        /// <summary>
        /// Normalizes a multiline SQL string by trimming each line and removing blank lines.
        /// </summary>
        /// <param name="input">The SQL text to normalize. CRLF, LF, and CR line breaks are accepted.</param>
        /// <returns>The normalized SQL text, with each line ending in <see cref="Environment.NewLine"/>.</returns>
        public static string ToSql(this string input)
        {
            var stringBuilder = new StringBuilder();
            string[] lines = input.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);

            foreach (string line in lines)
            {
                string trimmed = line.Trim();

                if (trimmed.Length > 0)
                {
                    stringBuilder.AppendLine(trimmed);
                }
            }

            return stringBuilder.ToString();
        }
    }
}
