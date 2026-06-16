using Microsoft.Extensions.Logging.Abstractions;
using WeCms.Persistence.Data;
using WeCms.Persistence.Modules.System.System;

namespace WeCms.Tests.Integration.SystemApi;

public sealed class SystemDatabaseProbeTests
{
    [Fact]
    public async Task CheckAsync_ReturnsAvailableForReachableDatabase()
    {
        var connectionString = RequiredConnectionString();
        using var db = new SqlSugarClientFactory(connectionString).Create();
        var probe = new SystemDatabaseProbe(db, NullLogger<SystemDatabaseProbe>.Instance);

        var result = await probe.CheckAsync(CancellationToken.None);

        Assert.True(result.Available);
        Assert.Null(result.FailureCode);
    }

    [Fact]
    public async Task CheckAsync_ReturnsUnavailableWithoutLeakingExceptionMessage()
    {
        using var db = new SqlSugarClientFactory("server=127.0.0.1;port=1;database=missing;uid=missing;pwd=missing;").Create();
        var probe = new SystemDatabaseProbe(db, NullLogger<SystemDatabaseProbe>.Instance);

        var result = await probe.CheckAsync(CancellationToken.None);

        Assert.False(result.Available);
        Assert.Equal("database_unavailable", result.FailureCode);
    }

    private static string RequiredConnectionString()
    {
        var connectionString = Environment.GetEnvironmentVariable("WECMS_TEST_MYSQL_CONNECTION_STRING");
        Assert.False(
            string.IsNullOrWhiteSpace(connectionString),
            "Set WECMS_TEST_MYSQL_CONNECTION_STRING to run MySQL integration tests.");

        return connectionString;
    }
}
