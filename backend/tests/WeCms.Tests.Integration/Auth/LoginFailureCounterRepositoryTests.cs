using SqlSugar;
using WeCms.Data.SqlSugar;
using WeCms.Shared.Security;

namespace WeCms.Tests.Integration.Auth;

[Collection(nameof(SharedMySqlCollection))]
public sealed class LoginFailureCounterRepositoryTests : PerTestDatabaseResetBase
{
    [DbFact]
    public async Task IncrementAsync_IncrementsWithinWindowAndResetClearsCounter()
    {
        using var db = new SqlSugarClientFactory(IntegrationTestDatabase.GetConnectionString()).Create();
        await PrepareDatabaseAsync(db);
        var repository = new LoginFailureCounterRepository(db, new SecurityEventClassifier());
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
