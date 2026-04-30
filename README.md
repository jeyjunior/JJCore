# JJCore

Biblioteca .NET 8 com recursos simples para conexão e operações de dados usando Dapper.

## Projetos

- `JJCore.Domain`: atributos, enums, DTOs, mensagens e extensões gerais.
- `JJCore.Infra.Data`: factory de conexão, unit of work, repositório base e extensões Dapper.

## Bancos suportados

- SQL Server (`Microsoft.Data.SqlClient`)
- SQLite (`Microsoft.Data.Sqlite`)

## Exemplo de entidade

```csharp
using JJCore.Domain.Atributos;

[Entidade("Clientes")]
public class Cliente
{
    [ChavePrimaria]
    public int Id { get; set; }

    [Obrigatorio]
    public string Nome { get; set; } = string.Empty;
}
```

## Exemplo de conexão

```csharp
using JJCore.Domain.Enumeradores;
using JJCore.Infra.Data.Data;
using JJCore.Infra.Data.Factory;

using var connection = ConnectionFactory.Criar("Data Source=app.db;", eTipoBancoDados.SQLite);
using var unitOfWork = new UnitOfWork(connection);
```

## Arquivo de parâmetros

```csharp
ConnectionFactory.GerarArquivoPadrao();
using var connection = ConnectionFactory.CriarDoArquivo("parametros.json");
```

O arquivo deve ter apenas uma conexão ativa.
