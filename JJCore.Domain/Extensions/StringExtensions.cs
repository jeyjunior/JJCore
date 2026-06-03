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
        /// Normalizes a multiline SQL string by trimming non-empty lines.
        /// </summary>
        /// <param name="input">The SQL text to normalize.</param>
        /// <returns>The normalized SQL text.</returns>
        public static string ToSql(this string input)
        {
            var stringBuilder = new StringBuilder();
            string[] lines = input.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines)
            {
                stringBuilder.AppendLine(line.Trim());
            }

            return stringBuilder.ToString();
        }
    }
}
