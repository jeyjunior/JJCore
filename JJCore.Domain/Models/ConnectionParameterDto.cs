using JJCore.Domain.Enums;

namespace JJCore.Domain.Models
{
    public class ConnectionParameterDto
    {
        public string ConnectionName { get; set; } = string.Empty;
        public string ConnectionString { get; set; } = string.Empty;
        public DatabaseType DatabaseType { get; set; }
        public bool IsActive { get; set; }
        public bool IsBase64String { get; set; }
    }

    public class ConnectionParametersFileDto
    {
        public List<ConnectionParameterDto> Connections { get; set; } = new List<ConnectionParameterDto>();
    }
}
