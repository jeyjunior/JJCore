using Dapper;
using JJCore.Domain.Attributes;
using JJCore.Domain.Enums;
using JJCore.Domain.Models;
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
        internal const string GetAsyncByIdObsoleteMessage =
            "Use GetByIdAsync. GetAsync will be removed in JJCore 3.0.";

        internal const string GetAsyncByConditionObsoleteMessage =
            "A string passed to GetAsync is executed as a WHERE condition, not as a key value. " +
            "Use GetByConditionAsync for conditions or GetByIdAsync for keys. GetAsync will be removed in JJCore 3.0.";

        private static readonly ConcurrentDictionary<Type, EntityMapping> Mappings = new();

        /// <summary>
        /// Gets one mapped entity by its primary key value.
        /// </summary>
        /// <typeparam name="T">The mapped entity type.</typeparam>
        /// <param name="connection">The database connection.</param>
        /// <param name="id">The primary key value. String values are always treated as keys, never as SQL.</param>
        /// <param name="transaction">The transaction to use, when provided.</param>
        /// <returns>The entity when found; otherwise, null.</returns>
        public static Task<T?> GetByIdAsync<T>(this IDbConnection connection, object id, IDbTransaction? transaction = null)
        {
            return connection.GetByIdAsync<T>(id, transaction, CancellationToken.None);
        }

        /// <summary>
        /// Gets one mapped entity by its primary key value with cancellation support.
        /// </summary>
        public static async Task<T?> GetByIdAsync<T>(this IDbConnection connection, object id, IDbTransaction? transaction, CancellationToken cancellationToken)
        {
            EnsureSupportedDatabase(connection, transaction);

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
        public static Task<T?> GetByConditionAsync<T>(this IDbConnection connection, string condition, object? parameters = null, IDbTransaction? transaction = null)
        {
            return connection.GetByConditionAsync<T>(condition, parameters, transaction, CancellationToken.None);
        }

        /// <summary>
        /// Gets one mapped entity using a custom WHERE condition with cancellation support.
        /// </summary>
        public static Task<T?> GetByConditionAsync<T>(this IDbConnection connection, string condition, object? parameters, IDbTransaction? transaction, CancellationToken cancellationToken)
        {
            return GetByConditionCoreAsync<T>(connection, condition, translateCondition: true, parameters, transaction, cancellationToken);
        }

        /// <summary>
        /// Gets one mapped entity using a custom WHERE condition without parameters, with cancellation support.
        /// </summary>
        public static Task<T?> GetByConditionAsync<T>(this IDbConnection connection, string condition, CancellationToken cancellationToken)
        {
            return GetByConditionCoreAsync<T>(connection, condition, translateCondition: true, null, null, cancellationToken);
        }

        /// <summary>
        /// Gets one mapped entity using a WHERE condition that is sent without identifier translation.
        /// </summary>
        /// <param name="connection">The database connection.</param>
        /// <param name="condition">The SQL WHERE condition without the WHERE keyword, written with native identifier quotes.</param>
        /// <param name="parameters">The query parameters.</param>
        /// <param name="transaction">The transaction to use, when provided.</param>
        /// <returns>The entity when found; otherwise, null.</returns>
        public static Task<T?> GetByConditionAsync<T>(this IDbConnection connection, RawSql condition, object? parameters = null, IDbTransaction? transaction = null)
        {
            return GetByConditionCoreAsync<T>(connection, condition.Text, translateCondition: false, parameters, transaction, CancellationToken.None);
        }

        /// <summary>
        /// Gets one mapped entity using a WHERE condition that is sent without identifier translation, with cancellation support.
        /// </summary>
        public static Task<T?> GetByConditionAsync<T>(this IDbConnection connection, RawSql condition, object? parameters, IDbTransaction? transaction, CancellationToken cancellationToken)
        {
            return GetByConditionCoreAsync<T>(connection, condition.Text, translateCondition: false, parameters, transaction, cancellationToken);
        }

        /// <summary>
        /// Gets one mapped entity by its primary key value.
        /// </summary>
        [Obsolete(GetAsyncByIdObsoleteMessage)]
        public static Task<T?> GetAsync<T>(this IDbConnection connection, object id, IDbTransaction? transaction = null)
        {
            return connection.GetByIdAsync<T>(id, transaction, CancellationToken.None);
        }

        /// <summary>
        /// Gets one mapped entity by its primary key value with cancellation support.
        /// </summary>
        [Obsolete(GetAsyncByIdObsoleteMessage)]
        public static Task<T?> GetAsync<T>(this IDbConnection connection, object id, IDbTransaction? transaction, CancellationToken cancellationToken)
        {
            return connection.GetByIdAsync<T>(id, transaction, cancellationToken);
        }

        /// <summary>
        /// Gets one mapped entity using a custom WHERE condition.
        /// </summary>
        [Obsolete(GetAsyncByConditionObsoleteMessage)]
        public static Task<T?> GetAsync<T>(this IDbConnection connection, string condition, object? parameters = null, IDbTransaction? transaction = null)
        {
            return connection.GetByConditionAsync<T>(condition, parameters, transaction, CancellationToken.None);
        }

        /// <summary>
        /// Gets one mapped entity using a custom WHERE condition with cancellation support.
        /// </summary>
        [Obsolete(GetAsyncByConditionObsoleteMessage)]
        public static Task<T?> GetAsync<T>(this IDbConnection connection, string condition, object? parameters, IDbTransaction? transaction, CancellationToken cancellationToken)
        {
            return connection.GetByConditionAsync<T>(condition, parameters, transaction, cancellationToken);
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
        public static Task<IEnumerable<T>> GetListAsync<T>(this IDbConnection connection, string condition, object? parameters, IDbTransaction? transaction, CancellationToken cancellationToken)
        {
            return GetListCoreAsync<T>(connection, condition, translateCondition: true, parameters, transaction, cancellationToken);
        }

        /// <summary>
        /// Gets mapped entities using an optional WHERE condition without parameters, with cancellation support.
        /// </summary>
        public static Task<IEnumerable<T>> GetListAsync<T>(this IDbConnection connection, string condition, CancellationToken cancellationToken)
        {
            return GetListCoreAsync<T>(connection, condition, translateCondition: true, null, null, cancellationToken);
        }

        /// <summary>
        /// Gets mapped entities using a WHERE condition that is sent without identifier translation.
        /// </summary>
        /// <param name="connection">The database connection.</param>
        /// <param name="condition">The SQL WHERE condition without the WHERE keyword, written with native identifier quotes.</param>
        /// <param name="parameters">The query parameters.</param>
        /// <param name="transaction">The transaction to use, when provided.</param>
        /// <returns>The matching entities.</returns>
        public static Task<IEnumerable<T>> GetListAsync<T>(this IDbConnection connection, RawSql condition, object? parameters = null, IDbTransaction? transaction = null)
        {
            return GetListCoreAsync<T>(connection, condition.Text, translateCondition: false, parameters, transaction, CancellationToken.None);
        }

        /// <summary>
        /// Gets mapped entities using a WHERE condition that is sent without identifier translation, with cancellation support.
        /// </summary>
        public static Task<IEnumerable<T>> GetListAsync<T>(this IDbConnection connection, RawSql condition, object? parameters, IDbTransaction? transaction, CancellationToken cancellationToken)
        {
            return GetListCoreAsync<T>(connection, condition.Text, translateCondition: false, parameters, transaction, cancellationToken);
        }

        private static async Task<T?> GetByConditionCoreAsync<T>(IDbConnection connection, string? condition, bool translateCondition, object? parameters, IDbTransaction? transaction, CancellationToken cancellationToken)
        {
            EnsureSupportedDatabase(connection, transaction);
            using var linkedCancellation = UnwrapCancellationToken(ref parameters, ref cancellationToken);

            if (string.IsNullOrWhiteSpace(condition))
            {
                throw new ArgumentException("A WHERE condition is required when searching without an ID.", nameof(condition));
            }

            string sql = BuildSelectSql(GetMapping(typeof(T)), condition, translateCondition, connection.IdentifyDatabase());

            var command = new CommandDefinition(sql, parameters, transaction, cancellationToken: cancellationToken);
            return await connection.QuerySingleOrDefaultAsync<T>(command).ConfigureAwait(false);
        }

        private static async Task<IEnumerable<T>> GetListCoreAsync<T>(IDbConnection connection, string? condition, bool translateCondition, object? parameters, IDbTransaction? transaction, CancellationToken cancellationToken)
        {
            EnsureSupportedDatabase(connection, transaction);
            using var linkedCancellation = UnwrapCancellationToken(ref parameters, ref cancellationToken);

            string sql = BuildSelectSql(GetMapping(typeof(T)), condition, translateCondition, connection.IdentifyDatabase());

            var command = new CommandDefinition(sql, parameters, transaction, cancellationToken: cancellationToken);
            return await connection.QueryAsync<T>(command).ConfigureAwait(false);
        }

        // The generated SELECT is always translated. A raw condition is appended after translation, so its brackets are kept.
        private static string BuildSelectSql(EntityMapping mapping, string? condition, bool translateCondition, DatabaseType databaseType)
        {
            string sql = $"SELECT {GetSelectColumns(mapping)} FROM {QuoteIdentifier(mapping.TableName)}";

            if (string.IsNullOrWhiteSpace(condition))
            {
                return SqlTranslator.TranslateQuery(sql, databaseType);
            }

            return translateCondition
                ? SqlTranslator.TranslateQuery($"{sql} WHERE {condition}", databaseType)
                : $"{SqlTranslator.TranslateQuery(sql, databaseType)} WHERE {condition}";
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
            EnsureSupportedDatabase(connection, transaction);

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

            var parameters = list.Select(entity => CreateParameters(entity, mapping.InsertProperties, databaseType)).ToList();
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
            if (entity != null)
            {
                EnsureSupportedDatabase(connection, transaction);
                EnsureKeyCanBeReturnedAsInt(entity, GetMapping(typeof(T)));
            }

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
            EnsureSupportedDatabase(connection, transaction);
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
                    CreateParameters(entity, mapping.InsertProperties, databaseType),
                    transaction,
                    cancellationToken: cancellationToken);
                await connection.ExecuteAsync(insertCommand).ConfigureAwait(false);

                return primaryKeyValue;
            }

            var parameters = CreateParameters(entity, mapping.InsertProperties, databaseType);
            string sql = BuildInsertReturningKeySql(mapping, databaseType);

            sql = SqlTranslator.TranslateQuery(sql, databaseType);

            var command = new CommandDefinition(sql, parameters, transaction, cancellationToken: cancellationToken);
            object? result = await connection.ExecuteScalarAsync<object>(command).ConfigureAwait(false);

            // MySQL returns 0 when the INSERT generated no AUTO_INCREMENT value, because LAST_INSERT_ID is reset before it.
            if (result == null || result == DBNull.Value ||
                (databaseType == DatabaseType.MySQL && Convert.ToDecimal(result, CultureInfo.InvariantCulture) == 0))
            {
                throw new InvalidOperationException(
                    $"The database did not return a generated key for {mapping.Type.Name}. The INSERT was executed, so the row may have been saved. " +
                    "Use [PrimaryKey(DatabaseGenerated = false)] when the application assigns the key value.");
            }

            return ConvertGeneratedKey(result, mapping);
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
            EnsureSupportedDatabase(connection, transaction);
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

            var parameters = CreateParameters(entity, mapping.WritableProperties, databaseType);
            var updateColumns = mapping.WritableProperties
                .Select(p => $"{QuoteIdentifier(p.ColumnName)} = @{p.Property.Name}");

            string keyParameterName = GetUniqueParameterName("JJCorePrimaryKey", mapping.WritableProperties);
            parameters.Add(keyParameterName, primaryKeyValue);

            string tableName = QuoteIdentifier(mapping.TableName);
            string primaryKeyName = QuoteIdentifier(mapping.PrimaryKey.ColumnName);
            string sql = $"UPDATE {tableName} SET {string.Join(", ", updateColumns)} WHERE {primaryKeyName} = @{keyParameterName}";

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
            EnsureSupportedDatabase(connection, transaction);

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
            EnsureSupportedDatabase(connection, transaction);

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
        public static Task<int> ExecuteQueryAsync(this IDbConnection connection, string query, object? parameters, IDbTransaction? transaction, CancellationToken cancellationToken)
        {
            return ExecuteQueryCoreAsync(connection, query, translateQuery: true, parameters, transaction, cancellationToken);
        }

        /// <summary>
        /// Executes a custom SQL command without parameters, with cancellation support.
        /// </summary>
        public static Task<int> ExecuteQueryAsync(this IDbConnection connection, string query, CancellationToken cancellationToken)
        {
            return ExecuteQueryCoreAsync(connection, query, translateQuery: true, null, null, cancellationToken);
        }

        /// <summary>
        /// Executes a custom SQL command that is sent without identifier translation.
        /// </summary>
        /// <param name="connection">The database connection.</param>
        /// <param name="query">The SQL command to execute, written with native identifier quotes.</param>
        /// <param name="parameters">The command parameters.</param>
        /// <param name="transaction">The transaction to use, when provided.</param>
        /// <returns>The number of affected rows.</returns>
        public static Task<int> ExecuteQueryAsync(this IDbConnection connection, RawSql query, object? parameters = null, IDbTransaction? transaction = null)
        {
            return ExecuteQueryCoreAsync(connection, query.Text, translateQuery: false, parameters, transaction, CancellationToken.None);
        }

        /// <summary>
        /// Executes a custom SQL command that is sent without identifier translation, with cancellation support.
        /// </summary>
        public static Task<int> ExecuteQueryAsync(this IDbConnection connection, RawSql query, object? parameters, IDbTransaction? transaction, CancellationToken cancellationToken)
        {
            return ExecuteQueryCoreAsync(connection, query.Text, translateQuery: false, parameters, transaction, cancellationToken);
        }

        private static async Task<int> ExecuteQueryCoreAsync(IDbConnection connection, string? query, bool translateQuery, object? parameters, IDbTransaction? transaction, CancellationToken cancellationToken)
        {
            EnsureSupportedDatabase(connection, transaction);
            using var linkedCancellation = UnwrapCancellationToken(ref parameters, ref cancellationToken);

            if (string.IsNullOrWhiteSpace(query))
            {
                throw new ArgumentException("Query cannot be null or empty.", nameof(query));
            }

            string sql = translateQuery ? SqlTranslator.TranslateQuery(query!, connection.IdentifyDatabase()) : query!;

            var command = new CommandDefinition(sql, parameters, transaction, cancellationToken: cancellationToken);
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
            EnsureSupportedDatabase(connection, transaction);

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

            return await connection.GetByConditionAsync<T>(condition, new { Code = code, UserId = userId }, transaction, cancellationToken).ConfigureAwait(false);
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

        private static void EnsureSupportedDatabase(IDbConnection connection, IDbTransaction? transaction = null)
        {
            var databaseType = connection.IdentifyDatabase();

            if (databaseType == DatabaseType.None)
            {
                throw new NotSupportedException(
                    $"Connection type {connection.GetType().Name} is not supported. For a wrapped connection or another driver, " +
                    "register its database with ConnectionFactory.RegisterDatabaseResolver.");
            }

            if (databaseType == DatabaseType.SQLite)
            {
                JJCoreTypeHandlers.EnsureSQLiteGuidHandler();
            }

            // A transaction without connection was committed, rolled back, or ended by the server. SqlClient would run the
            // command in autocommit mode instead of failing, so the command could not be undone.
            if (transaction != null && transaction.Connection == null)
            {
                throw new InvalidOperationException(
                    "The transaction is no longer usable: it was committed, rolled back, or ended by the database server. " +
                    "Roll back the unit of work and start a new transaction.");
            }
        }

        // A CancellationToken passed where the parameters object is expected, as in GetListAsync(condition, token), binds to
        // the overload without cancellation. It is used as the cancellation token instead of being sent as SQL parameters.
        private static CancellationTokenSource? UnwrapCancellationToken(ref object? parameters, ref CancellationToken cancellationToken)
        {
            if (parameters is not CancellationToken parametersToken)
            {
                return null;
            }

            parameters = null;

            if (!parametersToken.CanBeCanceled)
            {
                return null;
            }

            if (!cancellationToken.CanBeCanceled)
            {
                cancellationToken = parametersToken;
                return null;
            }

            var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, parametersToken);
            cancellationToken = linkedSource.Token;
            return linkedSource;
        }

        private static DynamicParameters CreateParameters<T>(T entity, IReadOnlyList<MappedProperty> properties, DatabaseType databaseType)
        {
            var parameters = new DynamicParameters();

            foreach (var property in properties)
            {
                var value = GetPreparedValue(entity, property.Property);

                // Without a type, SQL Server receives NULL as nvarchar, which it does not convert to varbinary.
                if (value == null && databaseType == DatabaseType.SQLServer && property.Property.PropertyType == typeof(byte[]))
                {
                    parameters.Add(property.Property.Name, null, DbType.Binary);
                    continue;
                }

                // Without a type, SqlClient sends DateTime as the legacy datetime type, which rounds to 1/300 second and
                // rejects dates before 1753, even for datetime2 and date columns.
                if (value is DateTime && databaseType == DatabaseType.SQLServer)
                {
                    parameters.Add(property.Property.Name, value, DbType.DateTime2);
                    continue;
                }

                parameters.Add(property.Property.Name, value);
            }

            return parameters;
        }

        // Names that JJCore adds to a command must not match a written property: an ordinary property named Id would
        // otherwise receive the key value in UpdateAsync.
        private static string GetUniqueParameterName(string baseName, IReadOnlyList<MappedProperty> properties)
        {
            string name = baseName;

            for (int suffix = 1; properties.Any(p => p.Property.Name.Equals(name, StringComparison.OrdinalIgnoreCase)); suffix++)
            {
                name = baseName + suffix.ToString(CultureInfo.InvariantCulture);
            }

            return name;
        }

        private static object? GetPreparedValue<T>(T entity, PropertyInfo property)
        {
            var value = property.GetValue(entity);

            // An unfilled DateTime or DateTimeOffset (default value) is written as NULL; JJCore never generates dates.
            if ((value is DateTime dateTime && dateTime == DateTime.MinValue) ||
                (value is DateTimeOffset dateTimeOffset && dateTimeOffset == DateTimeOffset.MinValue))
            {
                value = null;
            }

            if (value == null && property.GetCustomAttribute<RequiredFieldAttribute>() != null)
            {
                throw new InvalidOperationException($"Property {property.Name} is required and was not filled.");
            }

            return value;
        }

        private static string BuildInsertSql(EntityMapping mapping, string? outputClause = null)
        {
            string tableName = QuoteIdentifier(mapping.TableName);
            string columns = string.Join(", ", mapping.InsertProperties.Select(p => QuoteIdentifier(p.ColumnName)));
            string values = string.Join(", ", mapping.InsertProperties.Select(p => "@" + p.Property.Name));
            string output = outputClause == null ? string.Empty : outputClause + " ";

            return $"INSERT INTO {tableName} ({columns}) {output}VALUES ({values})";
        }

        private static string BuildInsertReturningKeySql(EntityMapping mapping, DatabaseType databaseType)
        {
            string insertSql = BuildInsertSql(mapping);
            string primaryKeyName = QuoteIdentifier(mapping.PrimaryKey.ColumnName);
            Type keyType = Nullable.GetUnderlyingType(mapping.PrimaryKey.Property.PropertyType) ?? mapping.PrimaryKey.Property.PropertyType;

            switch (databaseType)
            {
                case DatabaseType.SQLServer:
                    // SCOPE_IDENTITY() only knows IDENTITY columns. Guid and string keys come from a DEFAULT, so they are read
                    // with OUTPUT ... INTO, which also works on tables with triggers (OUTPUT without INTO does not).
                    string? keyColumnType = keyType == typeof(Guid) ? "uniqueidentifier" : keyType == typeof(string) ? "nvarchar(max)" : null;

                    if (keyColumnType == null)
                    {
                        return $"{insertSql}; SELECT CAST(SCOPE_IDENTITY() AS {GetSqlServerIdentityType(keyType)});";
                    }

                    string keyTable = "@" + GetUniqueParameterName("JJCoreKeys", mapping.InsertProperties);
                    string outputInsert = BuildInsertSql(mapping, $"OUTPUT INSERTED.{primaryKeyName} INTO {keyTable}");
                    return $"DECLARE {keyTable} TABLE ([Key] {keyColumnType}); {outputInsert}; SELECT [Key] FROM {keyTable};";

                case DatabaseType.SQLite:
                    // last_insert_rowid() returns the internal rowid, which is the key only for INTEGER PRIMARY KEY.
                    return $"{insertSql} RETURNING {primaryKeyName};";

                case DatabaseType.PostgreSQL:
                    return $"{insertSql} RETURNING {primaryKeyName};";

                case DatabaseType.MySQL:
                    // MySQL can only return AUTO_INCREMENT values. LAST_INSERT_ID() keeps the last value generated by the
                    // session, from any table, so it is reset first and a 0 result means that no key was generated.
                    if (!IsNumericType(keyType))
                    {
                        throw new InvalidOperationException(
                            $"MySQL cannot return a key generated by a column default or trigger for {mapping.Type.Name}.{mapping.PrimaryKey.Property.Name}. " +
                            "No row was inserted. Generate the key in the application and use [PrimaryKey(DatabaseGenerated = false)].");
                    }

                    return $"DO LAST_INSERT_ID(0); {insertSql}; SELECT LAST_INSERT_ID();";

                default:
                    throw new NotSupportedException($"Database {databaseType} is not supported.");
            }
        }

        private static bool IsNumericType(Type type)
        {
            return Type.GetTypeCode(type) switch
            {
                TypeCode.Byte or TypeCode.SByte or TypeCode.Int16 or TypeCode.UInt16 or TypeCode.Int32 or TypeCode.UInt32 or
                TypeCode.Int64 or TypeCode.UInt64 or TypeCode.Decimal => true,
                _ => false
            };
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

        // AddAsync returns the key as int. Rejects, before the INSERT, an assigned key that does not fit in int and a generated
        // key whose type never converts to int. A generated numeric or string key is only known after the INSERT.
        private static void EnsureKeyCanBeReturnedAsInt<T>(T entity, EntityMapping mapping)
        {
            Type keyType = Nullable.GetUnderlyingType(mapping.PrimaryKey.Property.PropertyType) ?? mapping.PrimaryKey.Property.PropertyType;
            bool fitsInInt;

            if (mapping.IsPrimaryKeyDatabaseGenerated)
            {
                fitsInInt = keyType != typeof(Guid) && keyType != typeof(DateTime) && keyType != typeof(DateTimeOffset) &&
                            keyType != typeof(TimeSpan) && keyType != typeof(byte[]);
            }
            else
            {
                object value = GetAssignedPrimaryKey(entity, mapping);

                try
                {
                    Convert.ToInt32(value, CultureInfo.InvariantCulture);
                    fitsInInt = true;
                }
                catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException)
                {
                    fitsInInt = false;
                }
            }

            if (!fitsInInt)
            {
                throw new InvalidOperationException(
                    $"AddAsync returns the key as int, and the {keyType.Name} key of {mapping.Type.Name} cannot be converted to int. " +
                    "Nothing was inserted. Use AddReturningAsync to read the key, or AddBatchAsync to insert without reading it.");
            }
        }

        // Converts the key returned by the database, such as UInt64 from MySQL or Int64 from SQLite, to the key property type.
        private static object ConvertGeneratedKey(object result, EntityMapping mapping)
        {
            Type keyType = Nullable.GetUnderlyingType(mapping.PrimaryKey.Property.PropertyType) ?? mapping.PrimaryKey.Property.PropertyType;

            if (keyType == typeof(object) || keyType.IsInstanceOfType(result))
            {
                return result;
            }

            try
            {
                if (keyType.IsEnum)
                {
                    return Enum.ToObject(keyType, Convert.ChangeType(result, Enum.GetUnderlyingType(keyType), CultureInfo.InvariantCulture));
                }

                if (keyType == typeof(Guid))
                {
                    return Guid.Parse(Convert.ToString(result, CultureInfo.InvariantCulture)!);
                }

                return Convert.ChangeType(result, keyType, CultureInfo.InvariantCulture);
            }
            catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException or ArgumentException)
            {
                throw new InvalidOperationException(
                    $"The database returned the key {result} ({result.GetType().Name}) for {mapping.Type.Name}, which cannot be converted to " +
                    $"{keyType.Name}. The INSERT was executed, so the row may have been saved.", ex);
            }
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
            // Static properties, indexers, and computed properties are not columns. [Column] keeps a computed property mapped.
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetIndexParameters().Length == 0 && p.GetCustomAttribute<NotMappedAttribute>() == null)
                .Where(p => p.GetCustomAttribute<ColumnAttribute>() != null || IsStoredProperty(p))
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

        // A property is stored when it has a setter of any visibility or a backing field generated by the compiler, which
        // Dapper fills for get-only auto-properties. A property with only a getter body, such as FullName => First + Last,
        // is computed. Private setters declared in a base class are only visible through the declaring type.
        private static bool IsStoredProperty(PropertyInfo property)
        {
            const BindingFlags DeclaredMembers = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;

            for (var type = property.DeclaringType; type != null; type = type.BaseType)
            {
                var declared = type.GetProperty(property.Name, DeclaredMembers, null, property.PropertyType, Type.EmptyTypes, null);

                if (declared?.SetMethod != null ||
                    type.GetField($"<{property.Name}>k__BackingField", DeclaredMembers) != null ||
                    type.GetField("_" + property.Name, DeclaredMembers) != null)
                {
                    return true;
                }
            }

            return false;
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
