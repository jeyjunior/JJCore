# JJCore

Simple .NET library for database connections and CRUD operations with Dapper.

## Projects

- `JJCore.Domain`: contracts, mapping attributes, DTOs, messages, and general utility extensions.
- `JJCore.Infra.Data.Core`: provider-neutral connection factory, unit of work, base repository, SQL translator, and Dapper extensions.
- `JJCore.Infra.Data.PostgreSQL`: PostgreSQL and Supabase provider package.
- `JJCore.Infra.Data.SqlServer`: SQL Server provider package.
- `JJCore.Infra.Data.SQLite`: SQLite provider package.
- `JJCore.Infra.Data.MySQL`: MySQL and MariaDB provider package.
- `JJCore.Infra.Data`: aggregate compatibility package containing all providers.

## Installation

Install only the provider used by the application:

```powershell
dotnet add package JJCore.Infra.Data.PostgreSQL --version 2.3.0
```

```powershell
dotnet add package JJCore.Infra.Data.SqlServer --version 2.3.0
```

```powershell
dotnet add package JJCore.Infra.Data.SQLite --version 2.3.0
```

```powershell
dotnet add package JJCore.Infra.Data.MySQL --version 2.3.0
```

Each provider package includes `JJCore.Infra.Data.Core` and `JJCore.Domain` transitively. Projects that use only domain contracts and utilities can reference `JJCore.Domain` directly.

The aggregate package remains available for applications that use multiple providers or need a controlled migration from version 1.x:

```powershell
dotnet add package JJCore.Infra.Data --version 2.3.0
```

Applications migrating from the aggregate package to a provider-specific package do not need namespace or code changes. Replace the package reference and restore the project.

### SQLite On .NET Framework

The native SQLite library is selected by process architecture. An `AnyCPU` .NET Framework executable runs as 64-bit, but the SDK copies only the x86 native library to its output, so the first connection fails with `Library e_sqlite3 not found`. This comes from `Microsoft.Data.Sqlite` and `SQLitePCLRaw`, not from JJCore. Set one of these properties in the executable project:

```xml
<PlatformTarget>x64</PlatformTarget>
<!-- or -->
<RuntimeIdentifier>win-x64</RuntimeIdentifier>
```

xUnit test projects on .NET Framework also need `"shadowCopy": false` in `xunit.runner.json`, or a `RuntimeIdentifier`, because shadow copying moves the test assembly away from the native library.

## Local Package Validation

Build all packages into the shared local source:

```powershell
dotnet pack .\JJCore.sln -c Release
```

Register the shared output directory as an additional NuGet source:

```powershell
dotnet nuget add source D:\path\to\JJCore\artifacts\packages --name JJCoreLocal
```

Install the unpublished provider package in a consumer project:

```powershell
dotnet add package JJCore.Infra.Data.PostgreSQL --version 2.3.0
dotnet add package JJCore.Infra.Data.MySQL --version 2.3.0
```

The `artifacts/packages` directory is generated locally and is not tracked by Git.

## Supported Databases

- SQL Server (`Microsoft.Data.SqlClient`)
- SQLite (`Microsoft.Data.Sqlite`)
- PostgreSQL/Supabase (`Npgsql`)
- MySQL/MariaDB (`MySqlConnector`)

`ConnectionFactory` reports which provider package is missing when an application requests a database provider that is not installed.

JJCore identifies the database by the connection type. A wrapped connection, such as MiniProfiler's `ProfiledDbConnection`, or a connection from another driver is rejected with `NotSupportedException` until its database is registered once at startup. The resolver is called only for connection types that JJCore does not recognize, and it applies to the whole process:

```csharp
using JJCore.Domain.Enums;
using JJCore.Infra.Data.Extensions;
using JJCore.Infra.Data.Factory;
using StackExchange.Profiling.Data;

ConnectionFactory.RegisterDatabaseResolver(connection =>
    connection is ProfiledDbConnection profiled
        ? profiled.WrappedConnection.IdentifyDatabase()
        : DatabaseType.None);
```

Only the four drivers listed above are tested. Other drivers, such as `MySql.Data` or `System.Data.SqlClient`, can be registered the same way, but their type conversions and transaction behavior may differ.

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

Use `[Column("column_name")]` when the C# property name is different from the database column name. Use `[NotMapped]` for properties that do not exist in the table. Static properties and indexers are never mapped.

A property with only a getter body, such as `public string FullName => FirstName + " " + LastName;`, is computed and is not mapped. Add `[Column]` to store a computed value in a column. Private setters, `init` setters, and get-only auto-properties (`{ get; }`) are mapped.

## Primary Keys

By default, the primary key is generated by the database (identity, auto-increment, serial, or a column default) and is not included in inserts. `AddReturningAsync` returns the generated value converted to the type of the key property, such as `int`, `long`, `short`, an enum, or `Guid`, so `long` keys are supported. If the value does not fit that type, it throws and says that the row may already have been saved. How the key is read depends on the database:

- SQL Server: numeric keys use `SCOPE_IDENTITY()`. `Guid` and `string` keys generated by a default, such as `NEWID()`, use `OUTPUT INSERTED ... INTO`, which also works on tables with triggers. A numeric key generated by a `SEQUENCE` default is not returned.
- SQLite: `INSERT ... RETURNING`, which requires SQLite 3.35 or later (the bundled SQLite is newer).
- PostgreSQL: `INSERT ... RETURNING`.
- MySQL: only `AUTO_INCREMENT` values can be returned. A `Guid` or `string` key generated by a default or trigger throws before the insert, so use `DatabaseGenerated = false` and generate the key in the application. A numeric key that does not come from `AUTO_INCREMENT` throws after the insert.

When the database does not return a key, `AddReturningAsync` throws and the message says whether the row may already have been saved.

`AddAsync` returns the key as `int`. It throws before the insert when the key cannot be an `int`: a key assigned by the application that does not fit, such as a `Guid`, a non-numeric `string`, or a `long` above `int.MaxValue`, and a database-generated key of a type that never converts, such as `Guid`. A database-generated `long` or `string` key is known only after the insert, so `AddAsync` throws after the row is saved when that value does not fit an `int`. Use `AddReturningAsync` for such keys, or `AddBatchAsync` to insert without reading the key.

SQLite stores `Guid` values as text. JJCore registers a Dapper type handler on the first SQLite operation so that `Guid` properties can be read, unless the application already registered a `Guid` handler. The handler applies to the whole process and does not convert `Guid` values stored as `BLOB`. SQLite compares text with case sensitivity, so a `Guid` written in lowercase by another tool is not found by `GetByIdAsync`.

When the application assigns the key, such as a `Guid` or a natural key, use `DatabaseGenerated = false`. The key is then included in inserts, and `AddReturningAsync` returns the assigned value. A key that is not filled (`null`, `Guid.Empty`, or an empty string) throws before the command runs.

```csharp
[Entity("Documents")]
public class Document
{
    [PrimaryKey(DatabaseGenerated = false)]
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Title { get; set; } = string.Empty;
}
```

## Dates

JJCore never fills dates automatically. A null `DateTime?` or `DateTimeOffset?`, an unfilled `DateTime` (`DateTime.MinValue`), and an unfilled `DateTimeOffset` (`DateTimeOffset.MinValue`) are written as `NULL` in inserts and updates. A `[RequiredField]` date that is not filled throws before the command runs.

SQL Server: inserts and updates send `DateTime` values as `datetime2`, so `datetime2` columns keep full precision and accept dates before 1753. Legacy `datetime`, `smalldatetime`, and `date` columns are converted by the server, rounding once instead of twice. Parameters passed by the application, such as `new { Moment = value }` in a condition, still use the Dapper default (`datetime`). To change them too, call `SqlMapper.AddTypeMap(typeof(DateTime), DbType.DateTime2)`, which applies to every provider in the process.

MySQL has no column type with an offset. MySqlConnector writes a `DateTimeOffset` as its UTC value but reads it back without kind, and Dapper then treats it as local time. Call `JJCoreTypeHandlers.ReadDateTimeOffsetAsUtc()` once at startup to read these values as UTC. The handler applies to every provider in the process: a PostgreSQL `timestamp` column read into a `DateTimeOffset` property is also treated as UTC. MySQL `timestamp` columns are converted by the server using the session time zone, which JJCore cannot correct; keep the session time zone at UTC or use `datetime`.

Columns filled by the database, such as `created_at DEFAULT CURRENT_TIMESTAMP`, should use `[Editable(false)]` so JJCore does not send them.

## Connection Example

```csharp
using JJCore.Domain.Enums;
using JJCore.Infra.Data.Data;
using JJCore.Infra.Data.Factory;

using var connection = ConnectionFactory.Create("Data Source=app.db;", DatabaseType.SQLite);
using var unitOfWork = new UnitOfWork(connection);
```

## MySQL/MariaDB Example

```csharp
using JJCore.Domain.Enums;
using JJCore.Infra.Data.Data;
using JJCore.Infra.Data.Factory;

string connectionString = "Server=MY_SERVER;Port=3306;Database=MY_DATABASE;User ID=MY_USER;Password=MY_PASSWORD;SslMode=Required;";

using var connection = ConnectionFactory.Create(connectionString, DatabaseType.MySQL);
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
Customer? customer = await repository.GetByIdAsync(1);
Customer? byEmail = await repository.GetByConditionAsync("[email] = @Email", new { Email = email });
IEnumerable<Customer> customers = await repository.GetListAsync();

int newId = await repository.AddAsync(new Customer
{
    Name = "Test Customer",
    UserId = 1,
    Code = 1
});
```

`GetByIdAsync` always treats its argument as a key value, including `string` keys. `GetByConditionAsync` executes its argument as a SQL `WHERE` condition, so always pass values through parameters.

`Repository<TEntity>.DeleteAsync` accepts keys of any type, such as `Guid` and `string`. Conditions without parameters accept a cancellation token directly, as in `GetListAsync("[active] = 1", cancellationToken)`, `GetByConditionAsync(condition, cancellationToken)`, and `ExecuteQueryAsync(query, cancellationToken)`.

These members, and the `RawSql` overloads, are declared in `IExtendedRepository<TEntity>`, which `Repository<TEntity>` implements. `IRepository<TEntity>` and `ICancellableRepository<TEntity>` are unchanged, so classes that implement them keep compiling. Inherit repository abstractions from `IExtendedRepository<TEntity>` to use the new members through dependency injection:

```csharp
public interface ICustomerRepository : IExtendedRepository<Customer>
{
}
```

Through `IExtendedRepository<TEntity>`, `DeleteAsync(5)` binds to `DeleteAsync(object)` and deletes the same row, and `GetListAsync(default)` binds to the `RawSql` overload and lists every row.

`GetAsync` is obsolete and will be removed in JJCore 3.0. Its `string` overload executes the argument as a `WHERE` condition, so `GetAsync("abc")` on an entity with a `string` key runs `WHERE abc` instead of searching by key. Replace `GetAsync(id)` with `GetByIdAsync(id)` and `GetAsync(condition)` with `GetByConditionAsync(condition)`.

## Custom SQL And Identifiers

Write identifiers with brackets, such as `[email]`, to keep conditions and custom SQL portable. SQL Server and SQLite support brackets natively. For PostgreSQL and MySQL, JJCore converts them to `"email"` and `` `email` ``.

Brackets inside string literals, quoted identifiers, comments, and PostgreSQL dollar-quoted strings are preserved. MySQL has no arrays: brackets are translated after any character, as in `WHERE[id]`, and also inside executable comments, such as `/*!80000 ... */`.

In PostgreSQL, these brackets are array syntax and are preserved:

- a bracket directly after a name, a closing parenthesis, or another bracket, such as `items[1]`, `(ARRAY[1, 2])[1]`, or `ARRAY[[1, 2]]`;
- a bracket after the `ARRAY` keyword, with or without spaces, such as `ARRAY [name]`;
- the elements of an array whose first element is itself an array, such as `[b]` in `ARRAY[[a], [b]]`.

A bracket directly after a reserved keyword, as in `WHERE[id]` or `AND[active]`, is an identifier. A bracket after a space and a name, a closing parenthesis, or a quoted identifier is also an identifier, because it cannot be told apart from an alias without `AS`, such as `FROM [Customers] [c]` or `FROM (SELECT ...) [sub]`. A subscript written with a space, such as `tags [i]`, is therefore translated to `tags "i"`; remove the space or use `RawSql`. Inside a one-dimensional array, `ARRAY[1, [code]]` translates `[code]`.

PostgreSQL treats quoted identifiers as case-sensitive. `[Customers]` becomes `"Customers"`, which does not match a table created as `customers`.

Use `RawSql` when a condition or command must be sent without translation, for example for a PostgreSQL subscript written with a space, such as `tags [i]`. Write native identifier quotes inside it. The SQL that JJCore generates, such as the select list, is still translated.

```csharp
using JJCore.Domain.Models;

var customers = await connection.GetListAsync<Customer>(new RawSql("'admin' = ANY(ARRAY [\"role\"])"));
await repository.ExecuteQueryAsync(new RawSql("UPDATE \"Customers\" SET \"tags\"[1] = 'a'"));
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

Commands that receive a transaction that was committed, rolled back, or ended by the server throw `InvalidOperationException` before they run. On SQL Server, `SET XACT_ABORT ON` errors and deadlocks end the transaction on the server; without this check, SqlClient would run the following commands outside the transaction. After such an error, roll back the unit of work and start again. MySQL ends the transaction on deadlocks, DDL statements, and `COMMIT` inside procedures without notifying the driver, and PostgreSQL rejects every command after an error and turns the next `COMMIT` into a rollback; in both cases, roll back after any error inside a transaction.

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

For concurrent applications, enforce a unique constraint and prefer a database sequence, identity, or another database-controlled strategy. If `GetNextCodeAsync` is used, duplicate-key conflicts must be handled by the application, or the call must run inside a transaction that takes a lock first: `SELECT ... FROM [table] WITH (TABLOCKX, HOLDLOCK)` on SQL Server, `LOCK TABLE` on PostgreSQL, or, on MySQL, `SELECT ... FOR UPDATE` on one row of another table used as a mutex (`FOR UPDATE` on the same table deadlocks).

`UnitOfWork` opens its connection in the constructor and keeps it, taken from the pool, until `Dispose`. Create it per operation and dispose it promptly, or use `AsyncUnitOfWork`, which opens only when `OpenAsync` is called.

## Database Behaviors

JJCore passes values to the driver and does not hide these database and driver behaviors:

- Batch inserts: `AddBatchAsync` without a transaction keeps the rows inserted before an error. Pass a transaction to insert all or nothing.
- Defaults: a column with a `DEFAULT` that is mapped without `[Editable(false)]` receives the value of the property. An unfilled `DateTime` is sent as `NULL`, so the default is not used.
- Lists of values: `IN @Ids` works on SQL Server, SQLite, and MySQL. PostgreSQL needs `= ANY(@Ids)`. SQL Server accepts at most 2100 parameters per command, which limits the size of `IN @Ids`.
- Cancellation: cancelling a running command throws `SqlException` on SQL Server instead of `OperationCanceledException`. On MySQL, a cancelled `SLEEP` or `BENCHMARK` returns without an exception.
- `[Entity]` is not inherited: a derived class without its own `[Entity]` uses its class name as the table name. `[StringSize]`, `[DecimalPrecision]`, and `[Relationship]` describe the model and are not validated by JJCore.
- `SetPropertyValue` returns `false` and keeps the current value when it receives `null` or `DBNull` for a non-nullable value type, such as `int`.
- PostgreSQL: a `DateTime` without `Kind = Utc` written to a `timestamptz` column is converted with the session time zone, and Npgsql rejects a `DateTimeOffset` with an offset other than zero. Convert values to UTC first. A `string` written to a `jsonb` column and a .NET enum written to a PostgreSQL `enum` column are rejected, because the parameter has no database type; cast in SQL, as in `@Data::jsonb`.
- MySQL: a .NET enum written to an `ENUM` column is sent as its number, and MySQL uses it as a 1-based index, so `Status.Active = 1` is stored as the first option. Store enums in numeric columns, or as text.
- MySQL: with `UseAffectedRows=true` in the connection string, `UpdateAsync` of a row without changes throws "No rows were updated", because the server reports 0 affected rows.
- MySQL: text compared with a number is converted, so a key `"1abc"` finds the row with key 1. Double quotes delimit text, not identifiers, unless `ANSI_QUOTES` is enabled.
- MySQL: a key assigned as `0` on an `AUTO_INCREMENT` column is replaced by a generated value, but `AddReturningAsync` returns `0`.
- MySQL: with `NO_BACKSLASH_ESCAPES` in `sql_mode`, the bracket translator treats `\'` as an escape and can translate brackets inside a string literal. JJCore does not read the session `sql_mode`.

## License

JJCore is distributed under a custom proprietary non-commercial license. Personal, educational, evaluation, and internal use is allowed. Commercial use, redistribution, sublicensing, resale, or publishing modified copies requires prior written permission from the copyright holder.
