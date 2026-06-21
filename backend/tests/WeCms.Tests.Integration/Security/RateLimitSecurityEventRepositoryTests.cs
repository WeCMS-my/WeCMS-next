using SqlSugar;
using WeCms.Modules.Security;
using WeCms.Modules.Security.SqlSugar.Repositories;
using WeCms.Data.SqlSugar;
using WeCms.Shared.Security;

namespace WeCms.Tests.Integration.Security;

[Collection(nameof(SharedMySqlCollection))]
public sealed class RateLimitSecurityEventRepositoryTests : PerTestDatabaseResetBase
{
    [DbFact]
    public async Task RecordHitAsync_WritesClassifiedSecurityEvent()
    {
        var connectionString = IntegrationTestDatabase.GetConnectionString();
        using var db = new SqlSugarClientFactory(connectionString).Create();
        await PrepareDatabaseAsync(db);
        var repository = new RateLimitSecurityEventRepository(db, new SecurityEventClassifier());
        var now = new DateTimeOffset(2026, 6, 18, 0, 0, 0, TimeSpan.Zero);

        await repository.RecordHitAsync(
            new RateLimitSecurityEventRecord(
                "rate_limit_hit",
                RateLimitPolicyNames.AuthLogin,
                "POST",
                "/api/v1/auth/login",
                null,
                null,
                "192.168.1.10",
                "warning",
                "rate-limit",
                "Rate limit hit for auth_login_policy on POST /api/v1/auth/login.",
                "trace-rate",
                now),
            CancellationToken.None);

        Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM sys_security_event WHERE event_type = 'rate_limit_hit'"));
        Assert.Equal("warning", Scalar<string>(db, "SELECT severity FROM sys_security_event WHERE event_type = 'rate_limit_hit' LIMIT 1"));
        Assert.Equal("rate-limit", Scalar<string>(db, "SELECT source FROM sys_security_event WHERE event_type = 'rate_limit_hit' LIMIT 1"));
        Assert.Equal("trace-rate", Scalar<string>(db, "SELECT trace_id FROM sys_security_event WHERE event_type = 'rate_limit_hit' LIMIT 1"));
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
