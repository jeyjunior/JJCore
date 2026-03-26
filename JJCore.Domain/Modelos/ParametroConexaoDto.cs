using JJCore.Domain.Enumeradores;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public List<ParametroConexaoDto> Conexoes { get; set; } = new();
    }
}
