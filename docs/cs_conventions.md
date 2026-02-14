# C# 9 / .NET Coding Conventions (Brief)

## Type Safety & Boundaries

**Assume all external input is unknown until validated:** HTTP requests, env vars, DB rows, queues, files, etc. Validate/parse at the boundary, then map into domain types.

**Prefer named domain types over primitives:** IDs, money, and “special strings” shouldn’t be raw `string/int/decimal`.

```csharp
public readonly record struct UserId(Guid Value);

public readonly record struct Money(long Cents)
{
    public static Money FromDollars(decimal dollars) =>
        new Money((long)Math.Round(dollars * 100m));
}
```

**Avoid optional-field soup:** Model states explicitly (discriminated-union style) instead of “maybe this field is set”.

```csharp
public abstract record Job;
public sealed record PendingJob() : Job;
public sealed record DoneJob(string Result) : Job;
public sealed record FailedJob(string Error) : Job;

public static string Handle(Job job) => job switch
{
    PendingJob => "pending",
    DoneJob d  => d.Result,
    FailedJob f => throw new InvalidOperationException(f.Error),
    _ => throw new ArgumentOutOfRangeException(nameof(job))
};
```

**Don’t leak DB/API shapes into domain:** Use explicit DTOs and mapping at boundaries (controllers/repos). Don’t pass DB entities or raw API DTOs through business logic.

**Turn on Nullable Reference Types and respect them:** Treat nullable warnings as real bugs (`<Nullable>enable</Nullable>`).

## Modern C# 9 Defaults

### Records + `with` expressions

Use `record` for immutable data (DTOs, commands, query results). Prefer `with` over mutation.

```csharp
public record User(string Name, bool IsActive);

var updated = user with { IsActive = false };
```

### `init`-only setters

Prefer `init` for configuration/DTOs to allow object initialization without later mutation.

```csharp
public sealed class AppConfig
{
    public string Region { get; init; } = "";
    public int Port { get; init; }
}
```

### Target-typed `new`

Use when the type is obvious from the left side and it improves scanning.

```csharp
List<UserId> ids = new();
Dictionary<string, int> counts = new();
```

### `record struct` for strong primitives

For value objects (IDs, money, special scalars), prefer `readonly record struct`.

```csharp
public readonly record struct OrderId(Guid Value);
```

### Pattern matching upgrades

Prefer property patterns / relational patterns when they make decisions clearer.

```csharp
return job switch
{
    DoneJob { Result: { Length: > 0 } r } => r,
    FailedJob { Error: var e } => throw new InvalidOperationException(e),
    _ => ""
};
```

### Top-level programs

If you’re using top-level statements (common in modern ASP.NET Core templates), keep `Program` thin and push real logic into extension methods.

```csharp
var builder = WebApplication.CreateBuilder(args);
// builder.Services.AddX();

var app = builder.Build();
// app.MapX();

app.Run();
```

## Immutability & Bindings

**Prefer immutability:** `readonly`, `init`, `record`, and “create new” over mutation.

**Use `var` when the RHS is obvious; otherwise be explicit:** `var user = ...` is fine, `var x = Get()` (unclear type) maybe not.

**Avoid “declare then overwrite”:**

```csharp
// Bad
Config config = Config.Default;
config = await LoadConfigAsync();

// Good
var config = await LoadConfigAsync();
```

## Control Flow

**Use guard clauses / early returns** to keep the happy path flat.

```csharp
public async Task<User> GetUserAsync(UserId id, CancellationToken ct)
{
    if (id.Value == Guid.Empty) throw new ArgumentException("Empty id", nameof(id));

    var user = await _repo.FindAsync(id, ct);
    return user ?? throw new KeyNotFoundException($"User {id.Value} not found");
}
```

**Prefer switch expressions/pattern matching** for multi-branch logic and to make all cases visible in one place.

## Functions, Scope, and Structure

**Small, well-named methods.** If a block has a “mini purpose,” extract it.

**Keep scopes tight:** declare variables as late as possible; avoid reusing variables for different meanings.

**Prefer dependency injection + constructor injection** (especially in ASP.NET Core). Avoid service locator patterns.

## Naming

| Kind | Style | Examples |
|---|---|---|
| Types / Records / Enums | PascalCase | `User`, `CreateUserRequest`, `OrderStatus` |
| Methods | PascalCase verbs | `ParseConfig`, `LoadUserAsync` |
| Parameters / locals | camelCase | `userId`, `totalCents` |
| Private fields | `_camelCase` | `_httpClient`, `_repo` |
| Booleans | questions | `isValid`, `hasAccess`, `shouldRetry` |
| Async methods | suffix `Async` | `SaveAsync`, `FetchAsync` |

## Error Handling

**Be consistent:** catch at boundaries (controllers/background job entrypoints), add context, and log.  

**Exceptions are for exceptional cases.** For expected failures (validation, not-found, conflict), prefer returning a result shape and translate to HTTP at the boundary.

```csharp
try
{
    var raw = await File.ReadAllTextAsync(path, ct);
    return JsonSerializer.Deserialize<AppConfig>(raw)
        ?? throw new InvalidOperationException("Config deserialized to null");
}
catch (Exception ex)
{
    throw new InvalidOperationException($"Failed to load config from {path}", ex);
}
```

**Don’t swallow exceptions silently.** If you handle it, either:
- return a meaningful result (`TryX`, `Result<T>` pattern), or
- log + rethrow / translate with context.

## Async & Concurrency

**Use async all the way:** avoid `.Result` / `.Wait()`.

**Always accept and pass `CancellationToken`** in I/O paths (HTTP, DB, file, queues).

**Avoid sequential awaits when parallelism is intended:**

```csharp
var tasks = ids.Select(id => _service.FetchAsync(id, ct));
var results = await Task.WhenAll(tasks);
```

(For large batches: use throttling with `SemaphoreSlim`.)

## LINQ & Collections

**Keep LINQ readable:** name intermediate results; avoid “one-liner novels”.

**Avoid multiple enumerations** of expensive sequences (materialize with `ToList()` when needed).

**Prefer `IReadOnlyList<T>` / `IReadOnlyDictionary<TKey,TValue>`** for returned collections unless mutation is required.

## Testing

**Test behavior, not implementation.** Use AAA, and name tests by outcome.

- xUnit: `[Fact]` for single cases, `[Theory]` + `[InlineData]` for table-driven tests.

```csharp
public class PortValidatorTests
{
    [Theory]
    [InlineData(80, true)]
    [InlineData(0, false)]
    public void IsValidPort_returns_expected(int port, bool expected)
    {
        // Act
        var ok = PortValidator.IsValidPort(port);

        // Assert
        Assert.Equal(expected, ok);
    }
}
```

**Avoid flaky tests:** don’t depend on real time, randomness, network, or global state—inject clocks/RNG, use fakes.
