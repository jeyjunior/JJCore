using System.Globalization;
using System.Reflection;

namespace JJCore.Domain.Extensions
{
    /// <summary>
    /// Provides reflection-based object helper methods.
    /// </summary>
    public static class ObjectExtensions
    {
        /// <summary>
        /// Gets a property value by name.
        /// </summary>
        /// <param name="instance">The source object.</param>
        /// <param name="propertyName">The property name.</param>
        /// <returns>The property value, or null when the object or property is not found.</returns>
        public static object? GetPropertyValue(this object? instance, string propertyName)
        {
            if (instance == null || string.IsNullOrWhiteSpace(propertyName))
            {
                return null;
            }

            return instance.GetType().GetProperty(propertyName)?.GetValue(instance);
        }

        /// <summary>
        /// Gets a property value by name and converts it to the requested type.
        /// </summary>
        /// <typeparam name="T">The expected return type.</typeparam>
        /// <param name="instance">The source object.</param>
        /// <param name="propertyName">The property name.</param>
        /// <param name="defaultValue">The value returned when conversion fails or the property is not found.</param>
        /// <returns>The converted property value, or the default value.</returns>
        public static T? GetPropertyValue<T>(this object? instance, string propertyName, T? defaultValue = default)
        {
            object? value = instance.GetPropertyValue(propertyName);
            return value.ConvertTo(defaultValue);
        }

        /// <summary>
        /// Converts a value to the requested type or returns a fallback value.
        /// </summary>
        /// <typeparam name="T">The expected return type.</typeparam>
        /// <param name="value">The value to convert.</param>
        /// <param name="defaultValue">The value returned when conversion is not possible.</param>
        /// <returns>The converted value, or the fallback value.</returns>
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

        /// <summary>
        /// Converts a value to the requested type using invariant culture.
        /// </summary>
        /// <typeparam name="T">The expected return type.</typeparam>
        /// <param name="value">The value to convert.</param>
        /// <param name="defaultValue">The value returned when conversion is not possible.</param>
        /// <returns>The converted value, or the fallback value.</returns>
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

        /// <summary>
        /// Sets a writable property value by name.
        /// </summary>
        /// <param name="instance">The target object.</param>
        /// <param name="propertyName">The property name.</param>
        /// <param name="value">The value to assign.</param>
        /// <returns>
        /// True when the value is assigned; otherwise, false. Null or DBNull for a non-nullable value type, such as int,
        /// returns false and keeps the current value.
        /// </returns>
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

            // Null cannot be stored in a non-nullable value type; SetValue would store the default value instead.
            if ((value == null || value == DBNull.Value) &&
                property.PropertyType.IsValueType &&
                Nullable.GetUnderlyingType(property.PropertyType) == null)
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
