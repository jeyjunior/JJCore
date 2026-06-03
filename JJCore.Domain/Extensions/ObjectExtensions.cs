using System.Globalization;
using System.Reflection;

namespace JJCore.Domain.Extensions
{
    public static class ObjectExtensions
    {
        public static object? GetPropertyValue(this object? instance, string propertyName)
        {
            if (instance == null || string.IsNullOrWhiteSpace(propertyName))
            {
                return null;
            }

            return instance.GetType().GetProperty(propertyName)?.GetValue(instance);
        }

        public static T? GetPropertyValue<T>(this object? instance, string propertyName, T? defaultValue = default)
        {
            object? value = instance.GetPropertyValue(propertyName);
            return value.ConvertTo(defaultValue);
        }

        public static T GetValueOrDefault<T>(this object? value, T defaultValue)
        {
            if (value == null || value == DBNull.Value)
            {
                return defaultValue;
            }

            if (typeof(T) == typeof(string) &&
                value is string text &&
                string.IsNullOrWhiteSpace(text) &&
                defaultValue is string defaultText &&
                !string.IsNullOrWhiteSpace(defaultText))
            {
                return defaultValue;
            }

            return value.ConvertTo(defaultValue)!;
        }

        public static T? ConvertTo<T>(this object? value, T? defaultValue = default)
        {
            if (value == null || value == DBNull.Value)
            {
                return defaultValue;
            }

            try
            {
                if (value is T result)
                {
                    return result;
                }

                Type targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

                if (targetType.IsEnum)
                {
                    return (T)Enum.Parse(targetType, value.ToString()!, true);
                }

                if (targetType == typeof(Guid))
                {
                    return (T)(object)Guid.Parse(value.ToString()!);
                }

                return (T)Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
            }
            catch
            {
                return defaultValue;
            }
        }

        public static bool SetPropertyValue(this object? instance, string propertyName, object? value)
        {
            if (instance == null || string.IsNullOrWhiteSpace(propertyName))
            {
                return false;
            }

            PropertyInfo? property = instance.GetType().GetProperty(propertyName);
            if (property == null || !property.CanWrite)
            {
                return false;
            }

            try
            {
                Type targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                object? convertedValue = ConvertToType(value, targetType);
                property.SetValue(instance, convertedValue);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static object? ConvertToType(object? value, Type targetType)
        {
            if (value == null || value == DBNull.Value)
            {
                return null;
            }

            if (targetType.IsEnum)
            {
                return Enum.Parse(targetType, value.ToString()!, true);
            }

            if (targetType == typeof(Guid))
            {
                return Guid.Parse(value.ToString()!);
            }

            return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
        }
    }
}
