using Dapper;
using JJCore.Domain.Attributes;
using JJCore.Domain.Enums;
using JJCore.Infra.Data.Translators;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Npgsql;
using System.Collections.Concurrent;
using System.Data;
using System.Globalization;
using System.Reflection;

namespace JJCore.Infra.Data.Extensions
{
    public static class DapperExtensions
    {
        private static readonly ConcurrentDictionary<Type, EntityMapping> Mappings = new();

        public static async Task<T?> GetAsync<T>(this IDbConnection connection, object id, IDbTransaction? transaction = null)
        {
            EnsureSupportedDatabase(connection);

            var mapping = GetMapping(typeof(T));
            string tableName = QuoteIdentifier(mapping.TableName);
            string columns = GetSelectColumns(mapping);
            string primaryKeyName = QuoteIdentifier(mapping.PrimaryKey.ColumnName);
            string sql = $"SELECT {columns} FROM {tableName} WHERE {primaryKeyName} = @Id";

            sql = SqlTranslator.TranslateQuery(sql, connection.IdentifyDatabase());

            return await connection.QuerySingleOrDefaultAsync<T>(sql, new { Id = id }, transaction);
        }

        public static async Task<T?> GetAsync<T>(this IDbConnection connection, string condition, object? parameters = null, IDbTransaction? transaction = null)
        {
            EnsureSupportedDatabase(connection);

            if (string.IsNullOrWhiteSpace(condition))
            {
                throw new ArgumentException("A WHERE condition is required when searching without an ID.", nameof(condition));
            }

            var mapping = GetMapping(typeof(T));
            string tableName = QuoteIdentifier(mapping.TableName);
            string columns = GetSelectColumns(mapping);
            string sql = $"SELECT {columns} FROM {tableName} WHERE {condition}";

            sql = SqlTranslator.TranslateQuery(sql, connection.IdentifyDatabase());

            return await connection.QuerySingleOrDefaultAsync<T>(sql, parameters, transaction);
        }

        public static async Task<IEnumerable<T>> GetListAsync<T>(this IDbConnection connection, string condition = "", object? parameters = null, IDbTransaction? transaction = null)
        {
            EnsureSupportedDatabase(connection);

            var mapping = GetMapping(typeof(T));
            string tableName = QuoteIdentifier(mapping.TableName);
            string columns = GetSelectColumns(mapping);
            string sql = $"SELECT {columns} FROM {tableName}";

            if (!string.IsNullOrWhiteSpace(condition))
            {
                sql += $" WHERE {condition}";
            }

            sql = SqlTranslator.TranslateQuery(sql, connection.IdentifyDatabase());

            return await connection.QueryAsync<T>(sql, parameters, transaction);
        }

        public static async Task<int> AddBatchAsync<T>(this IDbConnection connection, IEnumerable<T> entities, IDbTransaction? transaction = null)
        {
            EnsureSupportedDatabase(connection);

            var list = entities?.ToList();
            if (list == null || list.Count == 0)
            {
                return 0;
            }

            var databaseType = connection.IdentifyDatabase();
            var mapping = GetMapping(typeof(T));

            EnsureWritableProperties(mapping.WritableProperties, mapping.Type);

            var columns = mapping.WritableProperties.Select(p => QuoteIdentifier(p.ColumnName));
            var parameterNames = mapping.WritableProperties.Select(p => "@" + p.Property.Name);
            var parameters = list.Select(entity => CreateParameters(entity, mapping.WritableProperties)).ToList();
            string tableName = QuoteIdentifier(mapping.TableName);
            string sql = $"INSERT INTO {tableName} ({string.Join(", ", columns)}) VALUES ({string.Join(", ", parameterNames)})";

            sql = SqlTranslator.TranslateQuery(sql, databaseType);

            return await connection.ExecuteAsync(sql, parameters, transaction);
        }

        public static async Task<int> AddAsync<T>(this IDbConnection connection, T entity, IDbTransaction? transaction = null)
        {
            object? result = await connection.AddReturningAsync(entity, transaction);

            try
            {
                return Convert.ToInt32(result, CultureInfo.InvariantCulture);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Returned key cannot be converted to int. Use AddReturningAsync for other key types.", ex);
            }
        }

        public static async Task<object?> AddReturningAsync<T>(this IDbConnection connection, T entity, IDbTransaction? transaction = null)
        {
            EnsureSupportedDatabase(connection);
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            var databaseType = connection.IdentifyDatabase();
            var mapping = GetMapping(typeof(T));

            EnsureWritableProperties(mapping.WritableProperties, mapping.Type);

            var columns = mapping.WritableProperties.Select(p => QuoteIdentifier(p.ColumnName));
            var parameterNames = mapping.WritableProperties.Select(p => "@" + p.Property.Name);
            var parameters = CreateParameters(entity, mapping.WritableProperties);
            string tableName = QuoteIdentifier(mapping.TableName);
            string primaryKeyName = QuoteIdentifier(mapping.PrimaryKey.ColumnName);
            string cols = string.Join(", ", columns);
            string vals = string.Join(", ", parameterNames);
            string sql = databaseType switch
            {
                DatabaseType.SQLServer => $"INSERT INTO {tableName} ({cols}) VALUES ({vals}); SELECT CAST(SCOPE_IDENTITY() as int);",
                DatabaseType.SQLite => $"INSERT INTO {tableName} ({cols}) VALUES ({vals}); SELECT last_insert_rowid();",
                DatabaseType.PostgreSQL => $"INSERT INTO {tableName} ({cols}) VALUES ({vals}) RETURNING {primaryKeyName};",
                _ => throw new NotSupportedException($"Database {databaseType} is not supported.")
            };

            sql = SqlTranslator.TranslateQuery(sql, databaseType);

            object? result = await connection.ExecuteScalarAsync<object>(sql, parameters, transaction);

            if (result == null || result == DBNull.Value)
            {
                throw new InvalidOperationException("No row was inserted into the database.");
            }

            return result;
        }

        public static async Task<int> UpdateAsync<T>(this IDbConnection connection, T entity, IDbTransaction? transaction = null)
        {
            EnsureSupportedDatabase(connection);
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            var databaseType = connection.IdentifyDatabase();
            var mapping = GetMapping(typeof(T));

            EnsureWritableProperties(mapping.WritableProperties, mapping.Type);

            var primaryKeyValue = mapping.PrimaryKey.Property.GetValue(entity);
            if (primaryKeyValue == null)
            {
                throw new InvalidOperationException($"Primary key {mapping.PrimaryKey.Property.Name} was not filled.");
            }

            var parameters = CreateParameters(entity, mapping.WritableProperties);
            var updateColumns = mapping.WritableProperties
                .Select(p => $"{QuoteIdentifier(p.ColumnName)} = @{p.Property.Name}");

            parameters.Add("Id", primaryKeyValue);

            string tableName = QuoteIdentifier(mapping.TableName);
            string primaryKeyName = QuoteIdentifier(mapping.PrimaryKey.ColumnName);
            string sql = $"UPDATE {tableName} SET {string.Join(", ", updateColumns)} WHERE {primaryKeyName} = @Id";

            sql = SqlTranslator.TranslateQuery(sql, databaseType);

            var rowsAffected = await connection.ExecuteAsync(sql, parameters, transaction);

            if (rowsAffected == 0)
            {
                throw new InvalidOperationException("No rows were updated. Verify whether the ID exists.");
            }

            return rowsAffected;
        }

        public static async Task<int> DeleteAsync<T>(this IDbConnection connection, object id, IDbTransaction? transaction = null)
        {
            EnsureSupportedDatabase(connection);

            var databaseType = connection.IdentifyDatabase();
            var mapping = GetMapping(typeof(T));
            string tableName = QuoteIdentifier(mapping.TableName);
            string primaryKeyName = QuoteIdentifier(mapping.PrimaryKey.ColumnName);
            string sql = $"DELETE FROM {tableName} WHERE {primaryKeyName} = @Id";

            sql = SqlTranslator.TranslateQuery(sql, databaseType);

            int rowsAffected = await connection.ExecuteAsync(sql, new { Id = id }, transaction);

            if (rowsAffected == 0)
            {
                throw new InvalidOperationException("The record to delete does not exist.");
            }

            return rowsAffected;
        }

        public static async Task<int> DeleteAllAsync<T>(this IDbConnection connection, IDbTransaction? transaction = null)
        {
            EnsureSupportedDatabase(connection);

            var databaseType = connection.IdentifyDatabase();
            var mapping = GetMapping(typeof(T));
            string tableName = QuoteIdentifier(mapping.TableName);
            string sql = $"DELETE FROM {tableName}";

            sql = SqlTranslator.TranslateQuery(sql, databaseType);

            return await connection.ExecuteAsync(sql, transaction: transaction);
        }

        public static async Task<int> ExecuteQueryAsync(this IDbConnection connection, string query, object? parameters = null, IDbTransaction? transaction = null)
        {
            EnsureSupportedDatabase(connection);

            if (string.IsNullOrWhiteSpace(query))
            {
                throw new ArgumentException("Query cannot be null or empty.", nameof(query));
            }

            query = SqlTranslator.TranslateQuery(query, connection.IdentifyDatabase());

            return await connection.ExecuteAsync(query, parameters, transaction);
        }

        public static async Task<long> GetNextCodeAsync<T>(this IDbConnection connection, long userId, long initialValue = 1, IDbTransaction? transaction = null)
        {
            EnsureSupportedDatabase(connection);

            var databaseType = connection.IdentifyDatabase();
            var mapping = GetMapping(typeof(T));
            var codeProperty = GetUserCodeProperty(mapping);
            var userProperty = GetUserIdProperty(mapping);
            string tableName = QuoteIdentifier(mapping.TableName);
            string codeColumn = QuoteIdentifier(codeProperty.ColumnName);
            string userColumn = QuoteIdentifier(userProperty.ColumnName);
            long baseValue = initialValue - 1;
            string sql = $"SELECT COALESCE(MAX({codeColumn}), @BaseValue) + 1 FROM {tableName} WHERE {userColumn} = @UserId";

            sql = SqlTranslator.TranslateQuery(sql, databaseType);

            var result = await connection.ExecuteScalarAsync<object>(sql, new { UserId = userId, BaseValue = baseValue }, transaction);

            return Convert.ToInt64(result, CultureInfo.InvariantCulture);
        }

        public static async Task<T?> GetByCodeAsync<T>(this IDbConnection connection, long code, long userId, IDbTransaction? transaction = null)
        {
            var mapping = GetMapping(typeof(T));
            var codeProperty = GetUserCodeProperty(mapping);
            var userProperty = GetUserIdProperty(mapping);
            string codeColumn = QuoteIdentifier(codeProperty.ColumnName);
            string userColumn = QuoteIdentifier(userProperty.ColumnName);
            string condition = $"{codeColumn} = @Code AND {userColumn} = @UserId";

            return await connection.GetAsync<T>(condition, new { Code = code, UserId = userId }, transaction);
        }

        public static async Task<IEnumerable<T>> GetListByUserAsync<T>(this IDbConnection connection, long userId, IDbTransaction? transaction = null)
        {
            var mapping = GetMapping(typeof(T));
            var userProperty = GetUserIdProperty(mapping);
            string userColumn = QuoteIdentifier(userProperty.ColumnName);
            string condition = $"{userColumn} = @UserId";

            return await connection.GetListAsync<T>(condition, new { UserId = userId }, transaction);
        }

        public static DatabaseType IdentifyDatabase(this IDbConnection connection)
        {
            return connection switch
            {
                SqlConnection => DatabaseType.SQLServer,
                SqliteConnection => DatabaseType.SQLite,
                NpgsqlConnection => DatabaseType.PostgreSQL,
                _ => DatabaseType.None
            };
        }

        private static void EnsureSupportedDatabase(IDbConnection connection)
        {
            if (connection.IdentifyDatabase() == DatabaseType.None)
            {
                throw new NotSupportedException($"Connection type {connection.GetType().Name} is not supported.");
            }
        }

        private static DynamicParameters CreateParameters<T>(T entity, IReadOnlyList<MappedProperty> properties)
        {
            var parameters = new DynamicParameters();

            foreach (var property in properties)
            {
                parameters.Add(property.Property.Name, GetPreparedValue(entity, property.Property));
            }

            return parameters;
        }

        private static object? GetPreparedValue<T>(T entity, PropertyInfo property)
        {
            var value = property.GetValue(entity);

            if (property.GetCustomAttribute<RequiredFieldAttribute>() != null && value == null)
            {
                throw new InvalidOperationException($"Property {property.Name} is required and was not filled.");
            }

            if (property.PropertyType == typeof(DateTime) && (value == null || (DateTime)value == DateTime.MinValue))
            {
                return DateTime.UtcNow;
            }

            return value;
        }

        private static string GetSelectColumns(EntityMapping mapping)
        {
            return string.Join(", ", mapping.ReadProperties
                .Select(p => $"{QuoteIdentifier(p.ColumnName)} AS {QuoteIdentifier(p.Property.Name)}"));
        }

        private static string QuoteIdentifier(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Identifier cannot be empty.", nameof(value));
            }

            var parts = value.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim());
            return string.Join(".", parts.Select(QuoteIdentifierPart));
        }

        private static string QuoteIdentifierPart(string value)
        {
            var name = value.Trim('[', ']');

            if (name.Length == 0 || name.Any(c => !char.IsLetterOrDigit(c) && c != '_'))
            {
                throw new ArgumentException($"Identifier '{value}' contains invalid characters.");
            }

            return $"[{name}]";
        }

        private static EntityMapping GetMapping(Type type)
        {
            return Mappings.GetOrAdd(type, CreateMapping);
        }

        private static EntityMapping CreateMapping(Type type)
        {
            var properties = type.GetProperties()
                .Where(p => p.GetCustomAttribute<NotMappedAttribute>() == null)
                .Select(p => new MappedProperty(p, GetColumnName(p)))
                .ToList();

            var primaryKey = properties.FirstOrDefault(p => p.Property.GetCustomAttribute<PrimaryKeyAttribute>() != null);

            if (primaryKey == null)
            {
                primaryKey = properties.FirstOrDefault(p =>
                    p.Property.Name.Equals("Id", StringComparison.OrdinalIgnoreCase) ||
                    p.Property.Name.Equals($"PK_{type.Name}", StringComparison.OrdinalIgnoreCase));
            }

            if (primaryKey == null)
            {
                throw new InvalidOperationException($"Primary key was not found in {type.Name}.");
            }

            var entityAttribute = type.GetCustomAttribute<EntityAttribute>();
            string? tableName = entityAttribute?.TableName;

            if (string.IsNullOrWhiteSpace(tableName))
            {
                tableName = type.Name;
            }

            var writableProperties = properties
                .Where(p =>
                    p.Property != primaryKey.Property &&
                    p.Property.GetCustomAttribute<PrimaryKeyAttribute>() == null &&
                    p.Property.GetCustomAttribute<EditableAttribute>()?.Enabled != false)
                .ToList();

            return new EntityMapping(type, tableName!, primaryKey, properties, writableProperties);
        }

        private static string GetColumnName(PropertyInfo property)
        {
            return property.GetCustomAttribute<ColumnAttribute>()?.Name ?? property.Name;
        }

        private static MappedProperty GetUserCodeProperty(EntityMapping mapping)
        {
            return mapping.ReadProperties.FirstOrDefault(p => p.Property.GetCustomAttribute<UserCodeAttribute>() != null)
                ?? mapping.ReadProperties.FirstOrDefault(p => p.Property.Name.Equals("Code", StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException($"Code property was not found in {mapping.Type.Name}.");
        }

        private static MappedProperty GetUserIdProperty(EntityMapping mapping)
        {
            return mapping.ReadProperties.FirstOrDefault(p => p.Property.GetCustomAttribute<UserIdAttribute>() != null)
                ?? mapping.ReadProperties.FirstOrDefault(p => p.Property.Name.Equals("UserId", StringComparison.OrdinalIgnoreCase))
                ?? mapping.ReadProperties.FirstOrDefault(p => p.Property.Name.Equals("FK_User", StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException($"User ID property was not found in {mapping.Type.Name}.");
        }

        private static void EnsureWritableProperties(IReadOnlyCollection<MappedProperty> properties, Type type)
        {
            if (properties.Count == 0)
            {
                throw new InvalidOperationException($"No writable properties were found in {type.Name}.");
            }
        }

        private sealed class EntityMapping
        {
            public EntityMapping(
                Type type,
                string tableName,
                MappedProperty primaryKey,
                IReadOnlyList<MappedProperty> readProperties,
                IReadOnlyList<MappedProperty> writableProperties)
            {
                Type = type;
                TableName = tableName;
                PrimaryKey = primaryKey;
                ReadProperties = readProperties;
                WritableProperties = writableProperties;
            }

            public Type Type { get; }
            public string TableName { get; }
            public MappedProperty PrimaryKey { get; }
            public IReadOnlyList<MappedProperty> ReadProperties { get; }
            public IReadOnlyList<MappedProperty> WritableProperties { get; }
        }

        private sealed class MappedProperty
        {
            public MappedProperty(PropertyInfo property, string columnName)
            {
                Property = property;
                ColumnName = columnName;
            }

            public PropertyInfo Property { get; }
            public string ColumnName { get; }
        }
    }
}
