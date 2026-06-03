using System.Text;
using System.Text.RegularExpressions;

namespace JJCore.Domain.Extensions
{
    public static class StringExtensions
    {
        private static readonly Regex SafeNameRegex = new(@"^[\p{L}0-9\s\-\']+$", RegexOptions.Compiled);

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
