using Dapper;
using JJCore.Domain.Attributes;
using JJCore.Domain.Enums;
using JJCore.Infra.Data.Factory;
using JJCore.Infra.Data.Translators;
using System.Collections.Concurrent;
using System.Data;
using System.Globalization;
using System.Reflection;

namespace JJCore.Infra.Data.Extensions
{
    /// <summary>
    /// Provides generic Dapper-based CRUD extensions for supported JJCore database connections.
    /// </summary>
    public static class DapperExtensions
    {
        private static readonly ConcurrentDictionary<Type, EntityMapping> Mappings = new();

        /// <summary>
        /// Gets one mapped entity by its primary key value.
        /// </summary>
        /// <typeparam name="T">The mapped entity type.</typeparam>
        /// <param name="connection">The database connection.</param>
        /// <param name="id">The primary key value.</param>
        /// <param name="transaction">The transaction to use, when provided.</param>
        /// <returns>The entity when found; otherwise, null.</returns>
        public static Task<T?> GetAsync<T>(this IDbConnection connection, object id, IDbTransaction? transaction = null)
        {
            return connection.GetAsync<T>(id, transaction, CancellationToken.None);
        }

        /// <summary>
        /// Gets one mapped entity by its primary key value with cancellation support.
        /// </summary>
        public static async Task<T?> GetAsync<T>(this IDbConnection connection, object id, IDbTransaction? transaction, CancellationToken cancellationToken)
        {
            EnsureSupportedDatabase(connection);

            var mapping = GetMapping(typeof(T));
            string tableName = QuoteIdentifier(mapping.TableName);
            string columns = GetSelectColumns(mapping);
            string primaryKeyName = QuoteIdentifier(mapping.PrimaryKey.ColumnName);
            string sql = $"SELECT {columns} FROM {tableName} WHERE {primaryKeyName} = @Id";

            sql = SqlTranslator.TranslateQuery(sql, connection.IdentifyDatabase());

            var command = new CommandDefinition(sql, new { Id = id }, transaction, cancellationToken: cancellationToken);
            return await connection.QuerySingleOrDefaultAsync<T>(command).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets one mapped entity using a custom WHERE condition.
        /// </summary>
        /// <typeparam name="T">The mapped entity type.</typeparam>
        /// <param name="connection">The database connection.</param>
        /// <param name="condition">The SQL WHERE condition without the WHERE keyword.</param>
        /// <param name="parameters">The query parameters.</param>
        /// <param name="transaction">The transaction to use, when provided.</param>
        /// <returns>The entity when found; otherwise, null.</returns>
        public static Task<T?> GetAsync<T>(this IDbConnection connection, string condition, object? parameters = null, IDbTransaction? transaction = null)
        {
            return connection.GetAsync<T>(condition, parameters, transaction, CancellationToken.None);
        }

        /// <summary>
        /// Gets one mapped entity using a custom WHERE condition with cancellation support.
        /// </summary>
        public static async Task<T?> GetAsync<T>(this IDbConnection connection, string condition, object? parameters, IDbTransaction? transaction, CancellationToken cancellationToken)
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

            var command = new CommandDefinition(sql, parameters, transaction, cancellationToken: cancellationToken);
            return await connection.QuerySingleOrDefaultAsync<T>(command).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets mapped entities using an optional WHERE condition.
        /// </summary>
        /// <typeparam name="T">The mapped entity type.</typeparam>
        /// <param name="connection">The database connection.</param>
        /// <param name="condition">The SQL WHERE condition without the WHERE keyword.</param>
        /// <param name="parameters">The query parameters.</param>
        /// <param name="transaction">The transaction to use, when provided.</param>
        /// <returns>The matching entities.</returns>
        public static Task<IEnumerable<T>> GetListAsync<T>(this IDbConnection connection, string condition = "", object? parameters = null, IDbTransaction? transaction = null)
        {
            return connection.GetListAsync<T>(condition, parameters, transaction, CancellationToken.None);
        }

        /// <summary>
        /// Gets mapped entities using an optional WHERE condition with cancellation support.
        /// </summary>
        public static async Task<IEnumerable<T>> GetListAsync<T>(this IDbConnection connection, string condition, object? parameters, IDbTransaction? transaction, CancellationToken cancellationToken)
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

            var command = new CommandDefinition(sql, parameters, transaction, cancellationToken: cancellationToken);
            return await connection.QueryAsync<T>(command).ConfigureAwait(false);
        }

        /// <summary>
        /// Inserts multiple mapped entities by executing the generated INSERT statement once per entity.
        /// </summary>
        /// <typeparam name="T">The mapped entity type.</typeparam>
        /// <param name="connection">The database connection.</param>
        /// <param name="entities">The entities to insert.</param>
        /// <param name="transaction">The transaction to use, when provided.</param>
        /// <returns>The number of affected rows.</returns>
        public static Task<int> AddBatchAsync<T>(this IDbConnection connection, IEnumerable<T> entities, IDbTransaction? transaction = null)
        {
            return connection.AddBatchAsync(entities, transaction, CancellationToken.None);
        }

        /// <summary>
        /// Inserts multiple mapped entities with cancellation support.
        /// </summary>
        public static async Task<int> AddBatchAsync<T>(this IDbConnection connection, IEnumerable<T> entities, IDbTransaction? transaction, CancellationToken cancellationToken)
        {
            EnsureSupportedDatabase(connection);

            var list = entities?.ToList();
            if (list == null || list.Count == 0)
            {
                return 0;
            }

            var databaseType = connection.IdentifyDatabase();
            var mapping = GetMapping(typeof(T));

            EnsureWritableProperties(mapping.InsertProperties, mapping.Type);

            if (!mapping.IsPrimaryKeyDatabaseGenerated)
            {
                foreach (var entity in list)
                {
                    GetAssignedPrimaryKey(entity, mapping);
                }
            }

            var parameters = list.Select(entity => CreateParameters(entity, mapping.InsertProperties)).ToList();
            string sql = SqlTranslator.TranslateQuery(BuildInsertSql(mapping), databaseType);

            var command = new CommandDefinition(sql, parameters, transaction, cancellationToken: cancellationToken);
            return await connection.ExecuteAsync(command).ConfigureAwait(false);
        }

        /// <summary>
        /// Inserts one mapped entity and returns its primary key as an integer.
        /// </summary>
        /// <typeparam name="T">The mapped entity type.</typeparam>
        /// <param name="connection">The database connection.</param>
        /// <param name="entity">The entity to insert.</param>
        /// <param name="transaction">The transaction to use, when provided.</param>
        /// <returns>The primary key converted to int.</returns>
        public static Task<int> AddAsync<T>(this IDbConnection connection, T entity, IDbTransaction? transaction = null)
        {
            return connection.AddAsync(entity, transaction, CancellationToken.None);
        }

        /// <summary>
        /// Inserts one mapped entity with cancellation support and returns its integer primary key.
        /// </summary>
        public static async Task<int> AddAsync<T>(this IDbConnection connection, T entity, IDbTransaction? transaction, CancellationToken cancellationToken)
        {
            object? result = await connection.AddReturningAsync(entity, transaction, cancellationToken).ConfigureAwait(false);

            try
            {
                return Convert.ToInt32(result, CultureInfo.InvariantCulture);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Returned key cannot be converted to int. Use AddReturningAsync for other key types.", ex);
            }
        }

        /// <summary>
        /// Inserts one mapped entity and returns its primary key without forcing an integer conversion.
        /// </summary>
        /// <typeparam name="T">The mapped entity type.</typeparam>
        /// <param name="connection">The database connection.</param>
        /// <param name="entity">The entity to insert.</param>
        /// <param name="transaction">The transaction to use, when provided.</param>
        /// <returns>The primary key generated by the database or assigned by the application.</returns>
        public static Task<object?> AddReturningAsync<T>(this IDbConnection connection, T entity, IDbTransaction? transaction = null)
        {
            return connection.AddReturningAsync(entity, transaction, CancellationToken.None);
        }

        /// <summary>
        /// Inserts one mapped entity with cancellation support and returns its primary key.
        /// </summary>
        public static async Task<object?> AddReturningAsync<T>(this IDbConnection connection, T entity, IDbTransaction? transaction, CancellationToken cancellationToken)
        {
            EnsureSupportedDatabase(connection);
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            var databaseType = connection.IdentifyDatabase();
            var mapping = GetMapping(typeof(T));

            EnsureWritableProperties(mapping.InsertProperties, mapping.Type);

            string insertSql = BuildInsertSql(mapping);

            if (!mapping.IsPrimaryKeyDatabaseGenerated)
            {
                var primaryKeyValue = GetAssignedPrimaryKey(entity, mapping);

                var insertCommand = new CommandDefinition(
                    SqlTranslator.TranslateQuery(insertSql, databaseType),
                    CreateParameters(entity, mapping.InsertProperties),
                    transaction,
                    cancellationToken: cancellationToken);
                await connection.ExecuteAsync(insertCommand).ConfigureAwait(false);

                return primaryKeyValue;
            }

            var parameters = CreateParameters(entity, mapping.InsertProperties);
            string primaryKeyName = QuoteIdentifier(mapping.PrimaryKey.ColumnName);
            string identityType = GetSqlServerIdentityType(mapping.PrimaryKey.Property.PropertyType);
            string sql = databaseType switch
            {
                DatabaseType.SQLServer => $"{insertSql}; SELECT CAST(SCOPE_IDENTITY() AS {identityType});",
                DatabaseType.SQLite => $"{insertSql}; SELECT last_insert_rowid();",
                DatabaseType.PostgreSQL => $"{insertSql} RETURNING {primaryKeyName};",
                DatabaseType.MySQL => $"{insertSql}; SELECT LAST_INSERT_ID();",
                _ => throw new NotSupportedException($"Database {databaseType} is not supported.")
            };

            sql = SqlTranslator.TranslateQuery(sql, databaseType);

            var command = new CommandDefinition(sql, parameters, transaction, cancellationToken: cancellationToken);
            object? result = await connection.ExecuteScalarAsync<object>(command).ConfigureAwait(false);

            if (result == null || result == DBNull.Value)
            {
                throw new InvalidOperationException(
                    $"The database did not return a generated key for {mapping.Type.Name}. " +
                    "Use [PrimaryKey(DatabaseGenerated = false)] when the application assigns the key value.");
            }

            return result;
        }

        /// <summary>
        /// Updates one mapped entity using its primary key value.
        /// </summary>
        /// <typeparam name="T">The mapped entity type.</typeparam>
        /// <param name="connection">The database connection.</param>
        /// <param name="entity">The entity to update.</param>
        /// <param name="transaction">The transaction to use, when provided.</param>
        /// <returns>The number of affected rows.</returns>
        public static Task<int> UpdateAsync<T>(this IDbConnection connection, T entity, IDbTransaction? transaction = null)
        {
            return connection.UpdateAsync(entity, transaction, CancellationToken.None);
        }

        /// <summary>
        /// Updates one mapped entity with cancellation support.
        /// </summary>
        public static async Task<int> UpdateAsync<T>(this IDbConnection connection, T entity, IDbTransaction? transaction, CancellationToken cancellationToken)
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

            var command = new CommandDefinition(sql, parameters, transaction, cancellationToken: cancellationToken);
            var rowsAffected = await connection.ExecuteAsync(command).ConfigureAwait(false);

            if (rowsAffected == 0)
            {
                throw new InvalidOperationException("No rows were updated. Verify whether the ID exists.");
            }

            return rowsAffected;
        }

        /// <summary>
        /// Deletes one mapped entity by primary key value.
        /// </summary>
        /// <typeparam name="T">The mapped entity type.</typeparam>
        /// <param name="connection">The database connection.</param>
        /// <param name="id">The primary key value.</param>
        /// <param name="transaction">The transaction to use, when provided.</param>
        /// <returns>The number of affected rows.</returns>
        public static Task<int> DeleteAsync<T>(this IDbConnection connection, object id, IDbTransaction? transaction = null)
        {
            return connection.DeleteAsync<T>(id, transaction, CancellationToken.None);
        }

        /// <summary>
        /// Deletes one mapped entity by primary key value with cancellation support.
        /// </summary>
        public static async Task<int> DeleteAsync<T>(this IDbConnection connection, object id, IDbTransaction? transaction, CancellationToken cancellationToken)
        {
            EnsureSupportedDatabase(connection);

            var databaseType = connection.IdentifyDatabase();
            var mapping = GetMapping(typeof(T));
            string tableName = QuoteIdentifier(mapping.TableName);
            string primaryKeyName = QuoteIdentifier(mapping.PrimaryKey.ColumnName);
            string sql = $"DELETE FROM {tableName} WHERE {primaryKeyName} = @Id";

            sql = SqlTranslator.TranslateQuery(sql, databaseType);

            var command = new CommandDefinition(sql, new { Id = id }, transaction, cancellationToken: cancellationToken);
            int rowsAffected = await connection.ExecuteAsync(command).ConfigureAwait(false);

            if (rowsAffected == 0)
            {
                throw new InvalidOperationException("The record to delete does not exist.");
            }

            return rowsAffected;
        }

        /// <summary>
        /// Deletes all rows from the mapped table.
        /// </summary>
        /// <typeparam name="T">The mapped entity type.</typeparam>
        /// <param name="connection">The database connection.</param>
        /// <param name="transaction">The transaction to use, when provided.</param>
        /// <returns>The number of affected rows.</returns>
        public static Task<int> DeleteAllAsync<T>(this IDbConnection connection, IDbTransaction? transaction = null)
        {
            return connection.DeleteAllAsync<T>(transaction, CancellationToken.None);
        }

        /// <summary>
        /// Deletes all rows from the mapped table with cancellation support.
        /// </summary>
        public static async Task<int> DeleteAllAsync<T>(this IDbConnection connection, IDbTransaction? transaction, CancellationToken cancellationToken)
        {
            EnsureSupportedDatabase(connection);

            var databaseType = connection.IdentifyDatabase();
            var mapping = GetMapping(typeof(T));
            string tableName = QuoteIdentifier(mapping.TableName);
            string sql = $"DELETE FROM {tableName}";

            sql = SqlTranslator.TranslateQuery(sql, databaseType);

            var command = new CommandDefinition(sql, transaction: transaction, cancellationToken: cancellationToken);
            return await connection.ExecuteAsync(command).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes a custom SQL command with optional parameters.
        /// </summary>
        /// <param name="connection">The database connection.</param>
        /// <param name="query">The SQL command to execute.</param>
        /// <param name="parameters">The command parameters.</param>
        /// <param name="transaction">The transaction to use, when provided.</param>
        /// <returns>The number of affected rows.</returns>
        public static Task<int> ExecuteQueryAsync(this IDbConnection connection, string query, object? parameters = null, IDbTransaction? transaction = null)
        {
            return connection.ExecuteQueryAsync(query, parameters, transaction, CancellationToken.None);
        }

        /// <summary>
        /// Executes a custom SQL command with cancellation support.
        /// </summary>
        public static async Task<int> ExecuteQueryAsync(this IDbConnection connection, string query, object? parameters, IDbTransaction? transaction, CancellationToken cancellationToken)
        {
            EnsureSupportedDatabase(connection);

            if (string.IsNullOrWhiteSpace(query))
            {
                throw new ArgumentException("Query cannot be null or empty.", nameof(query));
            }

            query = SqlTranslator.TranslateQuery(query, connection.IdentifyDatabase());

            var command = new CommandDefinition(query, parameters, transaction, cancellationToken: cancellationToken);
            return await connection.ExecuteAsync(command).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets the next sequential code for a user-owned mapped entity.
        /// </summary>
        /// <typeparam name="T">The mapped entity type.</typeparam>
        /// <param name="connection">The database connection.</param>
        /// <param name="userId">The user identifier used to scope the sequence.</param>
        /// <param name="initialValue">The first value to use when no rows exist.</param>
        /// <param name="transaction">The transaction to use, when provided.</param>
        /// <returns>The next available code. The value is not reserved atomically and can collide under concurrent calls.</returns>
        public static Task<long> GetNextCodeAsync<T>(this IDbConnection connection, long userId, long initialValue = 1, IDbTransaction? transaction = null)
        {
            return connection.GetNextCodeAsync<T>(userId, initialValue, transaction, CancellationToken.None);
        }

        /// <summary>
        /// Gets the next sequential code with cancellation support.
        /// </summary>
        /// <returns>The next available code. The value is not reserved atomically and can collide under concurrent calls.</returns>
        public static async Task<long> GetNextCodeAsync<T>(this IDbConnection connection, long userId, long initialValue, IDbTransaction? transaction, CancellationToken cancellationToken)
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

            var command = new CommandDefinition(
                sql,
                new { UserId = userId, BaseValue = baseValue },
                transaction,
                cancellationToken: cancellationToken);
            var result = await connection.ExecuteScalarAsync<object>(command).ConfigureAwait(false);

            return Convert.ToInt64(result, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Gets one user-owned mapped entity by its per-user code.
        /// </summary>
        /// <typeparam name="T">The mapped entity type.</typeparam>
        /// <param name="connection">The database connection.</param>
        /// <param name="code">The per-user code value.</param>
        /// <param name="userId">The user identifier.</param>
        /// <param name="transaction">The transaction to use, when provided.</param>
        /// <returns>The entity when found; otherwise, null.</returns>
        public static Task<T?> GetByCodeAsync<T>(this IDbConnection connection, long code, long userId, IDbTransaction? transaction = null)
        {
            return connection.GetByCodeAsync<T>(code, userId, transaction, CancellationToken.None);
        }

        /// <summary>
        /// Gets one user-owned mapped entity by its per-user code with cancellation support.
        /// </summary>
        public static async Task<T?> GetByCodeAsync<T>(this IDbConnection connection, long code, long userId, IDbTransaction? transaction, CancellationToken cancellationToken)
        {
            var mapping = GetMapping(typeof(T));
            var codeProperty = GetUserCodeProperty(mapping);
            var userProperty = GetUserIdProperty(mapping);
            string codeColumn = QuoteIdentifier(codeProperty.ColumnName);
            string userColumn = QuoteIdentifier(userProperty.ColumnName);
            string condition = $"{codeColumn} = @Code AND {userColumn} = @UserId";

            return await connection.GetAsync<T>(condition, new { Code = code, UserId = userId }, transaction, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets all mapped entities owned by one user.
        /// </summary>
        /// <typeparam name="T">The mapped entity type.</typeparam>
        /// <param name="connection">The database connection.</param>
        /// <param name="userId">The user identifier.</param>
        /// <param name="transaction">The transaction to use, when provided.</param>
        /// <returns>The matching entities.</returns>
        public static Task<IEnumerable<T>> GetListByUserAsync<T>(this IDbConnection connection, long userId, IDbTransaction? transaction = null)
        {
            return connection.GetListByUserAsync<T>(userId, transaction, CancellationToken.None);
        }

        /// <summary>
        /// Gets all mapped entities owned by one user with cancellation support.
        /// </summary>
        public static async Task<IEnumerable<T>> GetListByUserAsync<T>(this IDbConnection connection, long userId, IDbTransaction? transaction, CancellationToken cancellationToken)
        {
            var mapping = GetMapping(typeof(T));
            var userProperty = GetUserIdProperty(mapping);
            string userColumn = QuoteIdentifier(userProperty.ColumnName);
            string condition = $"{userColumn} = @UserId";

            return await connection.GetListAsync<T>(condition, new { UserId = userId }, transaction, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Identifies the JJCore database provider represented by an open or closed connection instance.
        /// </summary>
        /// <param name="connection">The database connection.</param>
        /// <returns>The matching database type, or <see cref="DatabaseType.None"/> when unsupported.</returns>
        public static DatabaseType IdentifyDatabase(this IDbConnection connection)
        {
            return ConnectionFactory.IdentifyDatabase(connection);
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

            // An unfilled DateTime (default value) is written as NULL; JJCore never generates dates.
            if (value is DateTime dateTime && dateTime == DateTime.MinValue)
            {
                value = null;
            }

            if (value == null && property.GetCustomAttribute<RequiredFieldAttribute>() != null)
            {
                throw new InvalidOperationException($"Property {property.Name} is required and was not filled.");
            }

            return value;
        }

        private static string BuildInsertSql(EntityMapping mapping)
        {
            string tableName = QuoteIdentifier(mapping.TableName);
            string columns = string.Join(", ", mapping.InsertProperties.Select(p => QuoteIdentifier(p.ColumnName)));
            string values = string.Join(", ", mapping.InsertProperties.Select(p => "@" + p.Property.Name));

            return $"INSERT INTO {tableName} ({columns}) VALUES ({values})";
        }

        private static object GetAssignedPrimaryKey<T>(T entity, EntityMapping mapping)
        {
            var value = mapping.PrimaryKey.Property.GetValue(entity);

            if (value == null ||
                (value is Guid guid && guid == Guid.Empty) ||
                (value is string text && string.IsNullOrWhiteSpace(text)))
            {
                throw new InvalidOperationException($"Primary key {mapping.PrimaryKey.Property.Name} was not filled.");
            }

            return value;
        }

        private static string GetSqlServerIdentityType(Type keyType)
        {
            return Type.GetTypeCode(Nullable.GetUnderlyingType(keyType) ?? keyType) switch
            {
                TypeCode.Int64 or TypeCode.UInt32 or TypeCode.UInt64 => "bigint",
                TypeCode.Int16 => "smallint",
                TypeCode.Byte => "tinyint",
                TypeCode.Decimal => "decimal(38, 0)",
                _ => "int"
            };
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

            bool isPrimaryKeyDatabaseGenerated = primaryKey.Property.GetCustomAttribute<PrimaryKeyAttribute>()?.DatabaseGenerated ?? true;
            var insertProperties = isPrimaryKeyDatabaseGenerated
                ? writableProperties
                : new[] { primaryKey }.Concat(writableProperties).ToList();

            return new EntityMapping(type, tableName!, primaryKey, isPrimaryKeyDatabaseGenerated, properties, insertProperties, writableProperties);
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
                bool isPrimaryKeyDatabaseGenerated,
                IReadOnlyList<MappedProperty> readProperties,
                IReadOnlyList<MappedProperty> insertProperties,
                IReadOnlyList<MappedProperty> writableProperties)
            {
                Type = type;
                TableName = tableName;
                PrimaryKey = primaryKey;
                IsPrimaryKeyDatabaseGenerated = isPrimaryKeyDatabaseGenerated;
                ReadProperties = readProperties;
                InsertProperties = insertProperties;
                WritableProperties = writableProperties;
            }

            public Type Type { get; }
            public string TableName { get; }
            public MappedProperty PrimaryKey { get; }
            public bool IsPrimaryKeyDatabaseGenerated { get; }
            public IReadOnlyList<MappedProperty> ReadProperties { get; }
            public IReadOnlyList<MappedProperty> InsertProperties { get; }
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
