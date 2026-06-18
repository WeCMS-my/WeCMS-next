using SqlSugar;
using WeCms.Modules.System.Auth;
using WeCms.Persistence.Data;
using WeCms.Persistence.Migration;
using WeCms.Persistence.Modules.System.Auth;

namespace WeCms.Tests.Integration.Auth;

public sealed class LoginFailureCounterRepositoryTests : global::Xunit.IAsyncLifetime
{
    public Task InitializeAsync()
    {
        return IntegrationTestDatabase.ResetDatabaseAsync(IntegrationTestDatabase.GetConnectionString());
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [DbFact]
    public async Task IncrementAsync_IncrementsWithinWindowAndResetClearsCounter()
    {
        using var db = new SqlSugarClientFactory(IntegrationTestDatabase.GetConnectionString()).Create();
        await new DbMigrationRunner(db).MigrateAsync(RepoPath("database", "migrations"));
        var repository = new LoginFailureCounterRepository(db);
        var now = new DateTimeOffset(2026, 6, 18, 0, 0, 0, TimeSpan.Zero);

        var first = await repository.IncrementAsync(new LoginFailureCounterIncrement("username", "admin", now, now, TimeSpan.FromMinutes(10)), CancellationToken.None);
        var second = await repository.IncrementAsync(new LoginFailureCounterIncrement("username", "admin", now, now.AddMinutes(1), TimeSpan.FromMinutes(10)), CancellationToken.None);
        var third = await repository.IncrementAsync(new LoginFailureCounterIncrement("username", "admin", now, now.AddMinutes(2), TimeSpan.FromMinutes(10)), CancellationToken.None);

        Assert.Equal(1, first.FailureCount);
        Assert.Equal(2, second.FailureCount);
        Assert.Equal(3, third.FailureCount);

        await repository.ResetAsync("username", "admin", CancellationToken.None);
        Assert.Equal(0, Scalar<int>(db, "SELECT COUNT(1) FROM sys_login_failure_counter WHERE scope = 'username' AND target = 'admin'"));
    }

    private static T Scalar<T>(ISqlSugarClient db, string sql)
    {
        return (T)Convert.ChangeType(db.Ado.GetScalar(sql), typeof(T), global::System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string RepoPath(params string[] segments)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "backend", "WeCms.slnx")))
            {
                return Path.Combine([directory.FullName, .. segments]);
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
