using JJCore.Domain.Enumeradores;

namespace JJCore.Domain.Modelos
{
    public class ParametroConexaoDto
    {
        public string NomeConexao { get; set; } = string.Empty;
        public string Conexao { get; set; } = string.Empty;
        public eTipoBancoDados TipoBancoDados { get; set; }
        public bool Ativo { get; set; }
        public bool EhStringBase64 { get; set; }
    }
    public class ArquivoParametrosDto
    {
        public List<ParametroConexaoDto> Conexoes { get; set; } = new List<ParametroConexaoDto>();
    }
}
