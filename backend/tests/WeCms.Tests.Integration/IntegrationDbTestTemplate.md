# Integration DB Test Template (xUnit + DbFact)

Use this template when adding new integration tests that require MySQL.

## 1. Prefer `[DbFact]` over constructor guards

```csharp
using WeCms.Tests.Integration;

namespace WeCms.Tests.Integration.Feature;

public sealed class ExampleIntegrationTests : global::Xunit.IAsyncLifetime
{
    public Task InitializeAsync()
    {
        return IntegrationTestDatabase.ResetDatabaseAsync(RequiredConnectionString());
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [DbFact]
    public async Task Example_CanDoSomethingWithDatabase()
    {
        var baseConnectionString = RequiredConnectionString();

        try
        {
            using var db = new SqlSugarClientFactory(baseConnectionString).Create();
            await new DbMigrationRunner(db).MigrateAsync(RepoPath("database", "migrations"));
            await new SeedRunner(db).SeedAsync(RepoPath("database", "seeds"), new SeedRunnerOptions("Development", null));

            // test logic
        }
    }

    private static string RequiredConnectionString()
        => IntegrationTestDatabase.GetConnectionString();

    private static string RepoPath(params string[] segments)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "database"))
                && File.Exists(Path.Combine(directory.FullName, "backend", "WeCms.slnx")))
            {
                return Path.Combine([directory.FullName, .. segments]);
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
```

## 2. Why

- Keep tests stable in non-DB local environments.
- All DB integration tests in this suite should use `[DbFact]` so unavailable environments mark tests as `SKIP`.
- `DbFact` depends on `IntegrationTestDatabase.IsDatabaseAvailable(out reason)`.
- `InitializeAsync` should call `IntegrationTestDatabase.ResetDatabaseAsync(...)` and tests should use shared schema table cleanup strategy.

## 3. Do not use

- Per-test or constructor calls to `EnsureDatabaseAvailable()` / direct skip exceptions.
- Any hard DB assumptions without cleanup in `finally`.
