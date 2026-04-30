using System.Text.Json;

namespace JJCore.Domain.Extensao
{
    public static class JsonHelper
    {
        private static readonly JsonSerializerOptions LeituraOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private static readonly JsonSerializerOptions EscritaOptions = new()
        {
            WriteIndented = true
        };

        public static T? LerArquivo<T>(string caminhoArquivo)
        {
            if (!File.Exists(caminhoArquivo))
            {
                throw new FileNotFoundException($"O arquivo de configuração não foi encontrado: {caminhoArquivo}");
            }

            string json = File.ReadAllText(caminhoArquivo);
            return JsonSerializer.Deserialize<T>(json, LeituraOptions);
        }

        public static void SalvarArquivo<T>(T objeto, string caminhoArquivo)
        {
            string json = JsonSerializer.Serialize(objeto, EscritaOptions);
            File.WriteAllText(caminhoArquivo, json);
        }
    }
}
