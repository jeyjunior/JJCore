using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace JJCore.Domain.Extensao
{
    public static class StringExtension
    {
        public static bool EhNomeValido(this string texto, int tamanhoMinimo = 1, int tamanhoMaximo = 100)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return false;

            if (texto.Length < tamanhoMinimo || texto.Length > tamanhoMaximo)
                return false;

            // 2. Validação de Caracteres "Perigosos" ou Indesejados
            // A Regex abaixo permite:
            // ^ : Inicio
            // \w : Letras e Números (Alfanumérico)
            // \s : Espaços
            // À-nq-ÿ : Acentos (faixa unicode comum)
            // - : Hifens
            // ' : Apóstrofos (ex: D'água - isso é seguro com Dapper)
            // $ : Fim
            // Negação: Bloqueia < > ; -- (comentários SQL) / \ (barras)
            var regexSegura = new Regex(@"^[a-zA-Z0-9À-ÿ\u00f1\u00d1\s\-\']+$");

            if (!regexSegura.IsMatch(texto))
                return false;

            string upper = texto.ToUpper();
            if (upper.Contains("DROP TABLE") || upper.Contains("DELETE FROM") || upper.Contains("<SCRIPT>"))
                return false;

            return true;
        }

        public static string ToSQL(this string input)
        {
            StringBuilder stringBuilder = new StringBuilder();
            string[] array = input.Split(new string[1] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
            string[] array2 = array;
            foreach (string text in array2)
            {
                stringBuilder.AppendLine(text.Trim());
            }

            return stringBuilder.ToString();
        }
    }
}
