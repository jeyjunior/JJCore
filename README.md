# JJCore

Simple .NET library for database connections and CRUD operations with Dapper.

## Projects

- `JJCore.Domain`: contracts, mapping attributes, DTOs, messages, and general utility extensions.
- `JJCore.Infra.Data`: connection factory, unit of work, base repository, SQL translator, and Dapper extensions.

## Supported Databases

- SQL Server (`Microsoft.Data.SqlClient`)
- SQLite (`Microsoft.Data.Sqlite`)
- PostgreSQL/Supabase (`Npgsql`)

## Entity Example

```csharp
using JJCore.Domain.Attributes;

[Entity("Customers")]
public class Customer
{
    [PrimaryKey]
    public int Id { get; set; }

    [RequiredField, Column("name")]
    public string Name { get; set; } = string.Empty;

    [UserId, Column("user_id")]
    public long UserId { get; set; }

    [UserCode, Column("code")]
    public long Code { get; set; }

    [NotMapped]
    public string? CalculatedField { get; set; }
}
```

Use `[Column("column_name")]` when the C# property name is different from the database column name. Use `[NotMapped]` for properties that do not exist in the table.

## Connection Example

```csharp
using JJCore.Domain.Enums;
using JJCore.Infra.Data.Data;
using JJCore.Infra.Data.Factory;

using var connection = ConnectionFactory.Create("Data Source=app.db;", DatabaseType.SQLite);
using var unitOfWork = new UnitOfWork(connection);
```

## PostgreSQL/Supabase Example

```csharp
using JJCore.Domain.Enums;
using JJCore.Infra.Data.Data;
using JJCore.Infra.Data.Factory;

string connectionString = "Host=MY_SUPABASE_HOST;Port=5432;Database=postgres;Username=postgres;Password=MY_PASSWORD;SSL Mode=Require;Trust Server Certificate=true;";

using var connection = ConnectionFactory.Create(connectionString, DatabaseType.PostgreSQL);
using var unitOfWork = new UnitOfWork(connection);
```

## Repository Example

```csharp
using JJCore.Domain.Interfaces.Repository;
using JJCore.Infra.Data.Infrastructure;

public class CustomerRepository : Repository<Customer>
{
    public CustomerRepository(IUnitOfWork unitOfWork) : base(unitOfWork)
    {
    }
}
```

```csharp
Customer? customer = await repository.GetAsync(1);
IEnumerable<Customer> customers = await repository.GetListAsync();

int newId = await repository.AddAsync(new Customer
{
    Name = "Test Customer",
    UserId = 1,
    Code = 1
});
```

## Parameters File

```csharp
ConnectionFactory.GenerateDefaultFile();
using var connection = ConnectionFactory.CreateFromFile("parameters.json");
```

Example:

```json
{
  "Connections": [
    {
      "ConnectionName": "Local_SQLite",
      "ConnectionString": "Data Source=app.db;",
      "DatabaseType": 1,
      "IsActive": true,
      "IsBase64String": false
    }
  ]
}
```

The file must contain exactly one active connection.

## Transactions

```csharp
unitOfWork.BeginTransaction();

try
{
    await customerRepository.AddAsync(customer);
    await orderRepository.AddAsync(order);

    unitOfWork.Commit();
}
catch
{
    unitOfWork.Rollback();
    throw;
}
```

## License

JJCore is distributed under a custom proprietary non-commercial license. Personal, educational, evaluation, and internal use is allowed. Commercial use, redistribution, sublicensing, resale, or publishing modified copies requires prior written permission from the copyright holder.
