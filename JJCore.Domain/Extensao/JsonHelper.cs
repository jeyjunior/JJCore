using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace JJCore.Domain.Extensao
{
    public static class JsonHelper
    {
        public static T? LerArquivo<T>(string caminhoArquivo)
        {
            if (!File.Exists(caminhoArquivo))
                throw new FileNotFoundException($"O arquivo de configuração não foi encontrado: {caminhoArquivo}");

            string json = File.ReadAllText(caminhoArquivo);
            return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public static void SalvarArquivo<T>(T objeto, string caminhoArquivo)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(objeto, options);

            File.WriteAllText(caminhoArquivo, json);
        }
    }
}
