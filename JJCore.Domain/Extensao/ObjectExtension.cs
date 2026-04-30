using System.Globalization;
using System.Reflection;

namespace JJCore.Domain.Extensao
{
    public static class ObjectExtension
    {
        public static object? ObterPropriedade(this object? objeto, string nomePropriedade)
        {
            if (objeto == null || string.IsNullOrWhiteSpace(nomePropriedade))
            {
                return null;
            }

            return objeto.GetType().GetProperty(nomePropriedade)?.GetValue(objeto);
        }

        public static T? ObterPropriedade<T>(this object? objeto, string nomePropriedade, T? valorPadrao = default)
        {
            object? obj = objeto.ObterPropriedade(nomePropriedade);
            return obj.Converter(valorPadrao);
        }

        public static T ObterValorOuPadrao<T>(this object? valor, T padrao)
        {
            if (valor == null || valor == DBNull.Value)
            {
                return padrao;
            }

            if (typeof(T) == typeof(string) &&
                valor is string texto &&
                string.IsNullOrWhiteSpace(texto) &&
                padrao is string textoPadrao &&
                !string.IsNullOrWhiteSpace(textoPadrao))
            {
                return padrao;
            }

            return valor.Converter(padrao)!;
        }

        public static T? Converter<T>(this object? valor, T? padrao = default)
        {
            if (valor == null || valor == DBNull.Value)
            {
                return padrao;
            }

            try
            {
                if (valor is T result)
                {
                    return result;
                }

                Type tipoDestino = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

                if (tipoDestino.IsEnum)
                {
                    return (T)Enum.Parse(tipoDestino, valor.ToString()!, true);
                }

                if (tipoDestino == typeof(Guid))
                {
                    return (T)(object)Guid.Parse(valor.ToString()!);
                }

                return (T)Convert.ChangeType(valor, tipoDestino, CultureInfo.InvariantCulture);
            }
            catch
            {
                return padrao;
            }
        }

        public static bool DefinirValorParaPropriedade(this object? objeto, string nomePropriedade, object? valor)
        {
            if (objeto == null || string.IsNullOrWhiteSpace(nomePropriedade))
            {
                return false;
            }

            PropertyInfo? property = objeto.GetType().GetProperty(nomePropriedade);
            if (property == null || !property.CanWrite)
            {
                return false;
            }

            try
            {
                Type tipoDestino = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                object? valorConvertido = ConverterParaTipo(valor, tipoDestino);
                property.SetValue(objeto, valorConvertido);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static object? ConverterParaTipo(object? valor, Type tipoDestino)
        {
            if (valor == null || valor == DBNull.Value)
            {
                return null;
            }

            if (tipoDestino.IsEnum)
            {
                return Enum.Parse(tipoDestino, valor.ToString()!, true);
            }

            if (tipoDestino == typeof(Guid))
            {
                return Guid.Parse(valor.ToString()!);
            }

            return Convert.ChangeType(valor, tipoDestino, CultureInfo.InvariantCulture);
        }
    }
}
