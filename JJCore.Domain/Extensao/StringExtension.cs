using System.Text;
using System.Text.RegularExpressions;

namespace JJCore.Domain.Extensao
{
    public static class StringExtension
    {
        private static readonly Regex RegexNomeSeguro = new(@"^[a-zA-Z0-9À-ÿ\u00f1\u00d1\s\-\']+$", RegexOptions.Compiled);

        public static bool EhNomeValido(this string? texto, int tamanhoMinimo = 1, int tamanhoMaximo = 100)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                return false;
            }

            if (texto.Length < tamanhoMinimo || texto.Length > tamanhoMaximo)
            {
                return false;
            }

            if (!RegexNomeSeguro.IsMatch(texto))
            {
                return false;
            }

            string upper = texto.ToUpperInvariant();
            return !upper.Contains("DROP TABLE") &&
                   !upper.Contains("DELETE FROM") &&
                   !upper.Contains("<SCRIPT>");
        }

        public static string ToSQL(this string input)
        {
            var stringBuilder = new StringBuilder();
            string[] linhas = input.Split(["\n"], StringSplitOptions.RemoveEmptyEntries);

            foreach (string linha in linhas)
            {
                stringBuilder.AppendLine(linha.Trim());
            }

            return stringBuilder.ToString();
        }
    }
}
