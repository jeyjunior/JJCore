using JJCore.Domain.Enums;
using JJCore.Domain.Extensions;
using JJCore.Domain.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Npgsql;
using System.Data;
using System.Text;

namespace JJCore.Infra.Data.Factory
{
    public static class ConnectionFactory
    {
        public static IDbConnection Create(string connectionString, DatabaseType databaseType)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("Connection string cannot be empty.", nameof(connectionString));
            }

            return databaseType switch
            {
                DatabaseType.SQLServer => new SqlConnection(connectionString),
                DatabaseType.SQLite => new SqliteConnection(connectionString),
                DatabaseType.PostgreSQL => new NpgsqlConnection(connectionString),
                _ => throw new NotSupportedException($"Database {databaseType} is not supported.")
            };
        }

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

        public static void GenerateParametersFile(ConnectionParametersFileDto parameters, string filePath = "parameters.json")
        {
            if (parameters == null || parameters.Connections.Count == 0)
            {
                throw new ArgumentException("Connection list cannot be null or empty.", nameof(parameters));
            }

            JsonHelper.SaveFile(parameters, filePath);
        }

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
                    }
                }
            };

            JsonHelper.SaveFile(template, filePath);
        }
    }
}
