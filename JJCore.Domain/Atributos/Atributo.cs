namespace JJCore.Domain.Atributos
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ChavePrimaria : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class EntidadeAttribute : Attribute
    {
        public string? NomeTabela { get; }

        public EntidadeAttribute(string? nomeTabela = null)
        {
            NomeTabela = nomeTabela;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class Editavel : Attribute
    {
        public bool HabilitarEdicao { get; }

        public Editavel(bool valor)
        {
            HabilitarEdicao = valor;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class TamanhoString : Attribute
    {
        public int Tamanho { get; }

        public TamanhoString(int tamanho)
        {
            Tamanho = tamanho;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class TamanhoDecimal : Attribute
    {
        public int Tamanho { get; }
        public int Decimais { get; }

        public TamanhoDecimal(int tamanho, int decimais)
        {
            Tamanho = tamanho;
            Decimais = decimais;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class Relacionamento : Attribute
    {
        public string Tabela { get; }
        public string ChavePrimaria { get; }

        public Relacionamento(string tabela, string chavePrimaria = "Id")
        {
            Tabela = tabela;
            ChavePrimaria = chavePrimaria;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class Obrigatorio : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public sealed class CodigoGlyph : Attribute
    {
        public string Glyph { get; }
        public string FontFamily { get; }

        public CodigoGlyph(string glyph, string fontFamily)
        {
            Glyph = glyph;
            FontFamily = fontFamily;
        }
    }

    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public sealed class Fonte : Attribute
    {
        public string FontFamily { get; }

        public Fonte(string fontFamily)
        {
            FontFamily = fontFamily;
        }
    }
}
