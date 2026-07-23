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

## Asynchronous Unit Of Work

`AsyncUnitOfWork` does not open the connection in its constructor, which makes it suitable for dependency injection and Worker services. It implements both `IDisposable` and `IAsyncDisposable`.

```csharp
using var cancellationSource = new CancellationTokenSource();
CancellationToken cancellationToken = cancellationSource.Token;

await using var unitOfWork = new AsyncUnitOfWork(
    ConnectionFactory.Create(connectionString, DatabaseType.PostgreSQL));

await unitOfWork.OpenAsync(cancellationToken);
await unitOfWork.BeginTransactionAsync(cancellationToken);

try
{
    await customerRepository.AddAsync(customer, null, cancellationToken);
    await unitOfWork.CommitAsync(cancellationToken);
}
catch
{
    await unitOfWork.RollbackAsync(CancellationToken.None);
    throw;
}
```

For dependency injection, register `IAsyncUnitOfWork` as scoped and inject it into repositories that use cancellable operations. Create one scope per API request, Worker job, or logical operation. A Unit of Work must not be registered as a singleton or shared by parallel operations.

```csharp
services.AddScoped<IAsyncUnitOfWork>(_ => new AsyncUnitOfWork(
    ConnectionFactory.Create(connectionString, DatabaseType.PostgreSQL)));
services.AddScoped<ICustomerRepository, CustomerRepository>();
```

```csharp
public interface ICustomerRepository : ICancellableRepository<Customer>
{
}

public class CustomerRepository : Repository<Customer>, ICustomerRepository
{
    public CustomerRepository(IAsyncUnitOfWork unitOfWork) : base(unitOfWork)
    {
    }
}
```

Existing `IUnitOfWork`, `UnitOfWork`, `IRepository<TEntity>`, and repository methods without `CancellationToken` remain available. New repository abstractions can inherit from `ICancellableRepository<TEntity>` to expose the cancellable overloads.

Cancellation during `CommitAsync` does not prove that the database rejected the commit. Applications that require strong delivery guarantees must use idempotency and reconciliation appropriate to their domain.

## Sequential User Codes

`GetNextCodeAsync` calculates the next value with `MAX(code) + 1`. It is convenient for single-process or otherwise serialized operations, but it does not reserve the returned value atomically. Concurrent callers can receive the same value.

For concurrent applications, enforce a unique constraint and prefer a database sequence, identity, or another database-controlled strategy. If `GetNextCodeAsync` is used, duplicate-key conflicts must be handled by the application.

## License

JJCore is distributed under a custom proprietary non-commercial license. Personal, educational, evaluation, and internal use is allowed. Commercial use, redistribution, sublicensing, resale, or publishing modified copies requires prior written permission from the copyright holder.
