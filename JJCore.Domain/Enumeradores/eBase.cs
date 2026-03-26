using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JJCore.Domain.Enumeradores
{
    public enum eTipoBancoDados
    {
        Nenhum = 0,
        SQLite = 1,
        SQLServer = 2,
        MySQL = 3,
        PostgreSQL = 4
    }

    public enum eFormatacaoTexto
    {
        Letras = 1,
        LetrasNumeros = 2,
    }
}
