using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace JJCore.Domain.Extensao
{
    public static class ObjectExtension
    {
        public static object ObterPropriedade(this object objeto, string nomePropriedade)
        {
            if (objeto == null || string.IsNullOrWhiteSpace(nomePropriedade))
            {
                return null;
            }

            return objeto.GetType().GetProperty(nomePropriedade)?.GetValue(objeto);
        }

        public static T ObterPropriedade<T>(this object objeto, string nomePropriedade, T valorPadrao = default(T))
        {
            object obj = objeto.ObterPropriedade(nomePropriedade);
            if (obj == null)
            {
                return valorPadrao;
            }

            try
            {
                return (T)Convert.ChangeType(obj, typeof(T));
            }
            catch
            {
                return valorPadrao;
            }
        }

        public static T ObterValorOuPadrao<T>(this object? valor, T padrao)
        {
            if (valor == null || valor == DBNull.Value)
            {
                return padrao;
            }

            if (typeof(T) == typeof(string) && valor is string value && string.IsNullOrWhiteSpace(value) && padrao is string value2 && !string.IsNullOrWhiteSpace(value2))
            {
                return padrao;
            }

            try
            {
                if (valor is T result)
                {
                    return result;
                }

                return (T)Convert.ChangeType(valor, typeof(T), CultureInfo.InvariantCulture);
            }
            catch
            {
                return padrao;
            }
        }

        public static T Converter<T>(this object valor, T padrao = default(T))
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

                return (T)Convert.ChangeType(valor, typeof(T), CultureInfo.InvariantCulture);
            }
            catch
            {
                return padrao;
            }
        }

        public static bool DefinirValorParaPropriedade(this object objeto, string nomePropriedade, object valor)
        {
            if (objeto == null || string.IsNullOrWhiteSpace(nomePropriedade))
            {
                return false;
            }

            PropertyInfo property = objeto.GetType().GetProperty(nomePropriedade);
            if (property == null || !property.CanWrite)
            {
                return false;
            }

            try
            {
                object value = Convert.ChangeType(valor, property.PropertyType);
                property.SetValue(objeto, value);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
