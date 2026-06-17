# Integration DB Test Template (xUnit + DbFact)

Use this template when adding new integration tests that require MySQL.

## 1. Prefer `[DbFact]` over constructor guards

```csharp
using WeCms.Tests.Integration;

namespace WeCms.Tests.Integration.Feature;

public sealed class ExampleIntegrationTests
{
    [DbFact]
    public async Task Example_CanDoSomethingWithDatabase()
    {
        var baseConnectionString = RequiredConnectionString();
        var databaseName = $"wecms_example_{Guid.NewGuid():N}";

        using var serverClient = new SqlSugarClientFactory(baseConnectionString).Create();
        serverClient.Ado.ExecuteCommand($"DROP DATABASE IF EXISTS `{databaseName}`");
        serverClient.Ado.ExecuteCommand($"CREATE DATABASE `{databaseName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci");

        try
        {
            using var db = new SqlSugarClientFactory(WithDatabase(baseConnectionString, databaseName)).Create();
            await new DbMigrationRunner(db).MigrateAsync(RepoPath("database", "migrations"));

            // test logic
        }
        finally
        {
            serverClient.Ado.ExecuteCommand($"DROP DATABASE IF EXISTS `{databaseName}`");
        }
    }

    private static string RequiredConnectionString()
        => IntegrationTestDatabase.GetConnectionString();

    private static string WithDatabase(string connectionString, string databaseName)
    {
        var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(part => !part.StartsWith("database=", StringComparison.OrdinalIgnoreCase)
                && !part.StartsWith("initial catalog=", StringComparison.OrdinalIgnoreCase))
            .Append($"database={databaseName}");

        return string.Join(';', parts);
    }

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

## 3. Do not use

- Per-test or constructor calls to `EnsureDatabaseAvailable()` / direct skip exceptions.
- Any hard DB assumptions without cleanup in `finally`.
