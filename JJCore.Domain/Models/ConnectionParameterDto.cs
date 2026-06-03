using JJCore.Domain.Enums;

namespace JJCore.Domain.Models
{
    /// <summary>
    /// Represents one database connection entry in a JJCore parameters file.
    /// </summary>
    public class ConnectionParameterDto
    {
        /// <summary>
        /// Gets or sets a friendly name for the connection entry.
        /// </summary>
        public string ConnectionName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the database connection string.
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the database provider for this connection.
        /// </summary>
        public DatabaseType DatabaseType { get; set; }

        /// <summary>
        /// Gets or sets whether this connection is the active one.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets whether the connection string is stored as Base64 text.
        /// </summary>
        public bool IsBase64String { get; set; }
    }

    /// <summary>
    /// Represents the root structure of a JJCore database parameters file.
    /// </summary>
    public class ConnectionParametersFileDto
    {
        /// <summary>
        /// Gets or sets the available database connections.
        /// </summary>
        public List<ConnectionParameterDto> Connections { get; set; } = new List<ConnectionParameterDto>();
    }
}
