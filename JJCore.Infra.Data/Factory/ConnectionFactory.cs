using JJCore.Domain.Enums;
using JJCore.Domain.Extensions;
using JJCore.Domain.Models;
using System.Data;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;

namespace JJCore.Infra.Data.Factory
{
    /// <summary>
    /// Creates database connections supported by JJCore.
    /// </summary>
    public static class ConnectionFactory
    {
        private const string SqlServerConnectionType = "Microsoft.Data.SqlClient.SqlConnection, Microsoft.Data.SqlClient";
        private const string SQLiteConnectionType = "Microsoft.Data.Sqlite.SqliteConnection, Microsoft.Data.Sqlite";
        private const string PostgreSQLConnectionType = "Npgsql.NpgsqlConnection, Npgsql";
        private const string MySQLConnectionType = "MySqlConnector.MySqlConnection, MySqlConnector";

        /// <summary>
        /// Creates a database connection for the selected provider.
        /// </summary>
        /// <param name="connectionString">The database connection string.</param>
        /// <param name="databaseType">The database provider type.</param>
        /// <returns>A closed database connection instance.</returns>
        public static IDbConnection Create(string connectionString, DatabaseType databaseType)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("Connection string cannot be empty.", nameof(connectionString));
            }

            return databaseType switch
            {
                DatabaseType.SQLServer => CreateProviderConnection(connectionString, databaseType, SqlServerConnectionType, "JJCore.Infra.Data.SqlServer"),
                DatabaseType.SQLite => CreateProviderConnection(connectionString, databaseType, SQLiteConnectionType, "JJCore.Infra.Data.SQLite"),
                DatabaseType.PostgreSQL => CreateProviderConnection(connectionString, databaseType, PostgreSQLConnectionType, "JJCore.Infra.Data.PostgreSQL"),
                DatabaseType.MySQL => CreateProviderConnection(connectionString, databaseType, MySQLConnectionType, "JJCore.Infra.Data.MySQL"),
                _ => throw new NotSupportedException($"Database {databaseType} is not supported.")
            };
        }

        internal static DatabaseType IdentifyDatabase(IDbConnection connection)
        {
            return connection.GetType().FullName switch
            {
                "Microsoft.Data.SqlClient.SqlConnection" => DatabaseType.SQLServer,
                "Microsoft.Data.Sqlite.SqliteConnection" => DatabaseType.SQLite,
                "Npgsql.NpgsqlConnection" => DatabaseType.PostgreSQL,
                "MySqlConnector.MySqlConnection" => DatabaseType.MySQL,
                _ => DatabaseType.None
            };
        }

        /// <summary>
        /// Creates a database connection from a JJCore parameters JSON file.
        /// </summary>
        /// <param name="filePath">The parameters file path.</param>
        /// <returns>A closed database connection instance for the active configuration.</returns>
        public static IDbConnection CreateFromFile(string filePath = "parameters.json")
        {
            var file = JsonHelper.ReadFile<ConnectionParametersFileDto>(filePath);

            if (file == null || file.Connections.Count == 0)
            {
                throw new InvalidOperationException("Parameters file is empty or invalid.");
            }

            var activeConnections = file.Connections.Where(c => c.IsActive).ToList();

            if (activeConnections.Count == 0)
            {
                throw new InvalidOperationException("No active connection was found in the parameters file.");
            }

            if (activeConnections.Count > 1)
            {
                throw new InvalidOperationException("More than one active connection was found in the parameters file.");
            }

            var activeConfig = activeConnections[0];
            string finalConnectionString = activeConfig.ConnectionString;

            if (activeConfig.IsBase64String)
            {
                var base64Bytes = Convert.FromBase64String(finalConnectionString);
                finalConnectionString = Encoding.UTF8.GetString(base64Bytes);
            }

            return Create(finalConnectionString, activeConfig.DatabaseType);
        }

        /// <summary>
        /// Saves a JJCore parameters file.
        /// </summary>
        /// <param name="parameters">The connection parameters to save.</param>
        /// <param name="filePath">The destination file path.</param>
        public static void GenerateParametersFile(ConnectionParametersFileDto parameters, string filePath = "parameters.json")
        {
            if (parameters == null || parameters.Connections.Count == 0)
            {
                throw new ArgumentException("Connection list cannot be null or empty.", nameof(parameters));
            }

            JsonHelper.SaveFile(parameters, filePath);
        }

        /// <summary>
        /// Generates a sample JJCore parameters file with SQL Server, SQLite, PostgreSQL, and MySQL entries.
        /// </summary>
        /// <param name="filePath">The destination file path.</param>
        public static void GenerateDefaultFile(string filePath = "parameters.json")
        {
            var template = new ConnectionParametersFileDto
            {
                Connections = new List<ConnectionParameterDto>
                {
                    new ConnectionParameterDto
                    {
                        ConnectionName = "Production_SQLServer",
                        ConnectionString = "Server=MY_SERVER;Database=MY_DATABASE;User Id=MY_USER;Password=MY_PASSWORD;TrustServerCertificate=True;",
                        DatabaseType = DatabaseType.SQLServer,
                        IsActive = true,
                        IsBase64String = false
                    },
                    new ConnectionParameterDto
                    {
                        ConnectionName = "Local_SQLite",
                        ConnectionString = "Data Source=local_database.db;",
                        DatabaseType = DatabaseType.SQLite,
                        IsActive = false,
                        IsBase64String = false
                    },
                    new ConnectionParameterDto
                    {
                        ConnectionName = "Supabase_PostgreSQL",
                        ConnectionString = "Host=MY_SUPABASE_HOST;Port=5432;Database=postgres;Username=postgres;Password=MY_PASSWORD;SSL Mode=Require;Trust Server Certificate=true;",
                        DatabaseType = DatabaseType.PostgreSQL,
                        IsActive = false,
                        IsBase64String = false
                    },
                    new ConnectionParameterDto
                    {
                        ConnectionName = "Production_MySQL",
                        ConnectionString = "Server=MY_SERVER;Port=3306;Database=MY_DATABASE;User ID=MY_USER;Password=MY_PASSWORD;SslMode=Required;",
                        DatabaseType = DatabaseType.MySQL,
                        IsActive = false,
                        IsBase64String = false
                    }
                }
            };

            JsonHelper.SaveFile(template, filePath);
        }

        private static IDbConnection CreateProviderConnection(
            string connectionString,
            DatabaseType databaseType,
            string connectionTypeName,
            string packageName)
        {
            Type? connectionType = Type.GetType(connectionTypeName, throwOnError: false);

            if (connectionType == null)
            {
                throw new NotSupportedException(
                    $"Database provider {databaseType} is not installed. Add the {packageName} package.");
            }

            object? connection;

            try
            {
                connection = Activator.CreateInstance(connectionType, new object[] { connectionString });
            }
            catch (TargetInvocationException exception) when (exception.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
                throw;
            }

            return connection as IDbConnection
                ?? throw new InvalidOperationException(
                    $"The connection type provided by {packageName} does not implement IDbConnection.");
        }
    }
}
