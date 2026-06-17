using Microsoft.Extensions.Logging.Abstractions;
using WeCms.Persistence.Data;
using WeCms.Persistence.Modules.System.System;
using WeCms.Tests.Integration;

namespace WeCms.Tests.Integration.SystemApi;

public sealed class SystemDatabaseProbeTests
{

    [DbFact]
    public async Task CheckAsync_ReturnsAvailableForReachableDatabase()
    {
        var connectionString = RequiredConnectionString();
        using var db = new SqlSugarClientFactory(connectionString).Create();
        var probe = new SystemDatabaseProbe(db, NullLogger<SystemDatabaseProbe>.Instance);

        var result = await probe.CheckAsync(CancellationToken.None);

        Assert.True(result.Available);
        Assert.Null(result.FailureCode);
    }

    [DbFact]
    public async Task CheckAsync_ReturnsUnavailableWithoutLeakingExceptionMessage()
    {
        using var db = new SqlSugarClientFactory("server=192.168.101.199;port=3306;database=wecms_missing_db;uid=wecms_dev;pwd=wecms-dev-123;charset=utf8mb4;SslMode=None;").Create();
        var probe = new SystemDatabaseProbe(db, NullLogger<SystemDatabaseProbe>.Instance);

        var result = await probe.CheckAsync(CancellationToken.None);

        Assert.False(result.Available);
        Assert.Equal("database_unavailable", result.FailureCode);
    }

    private static string RequiredConnectionString()
    {
        return IntegrationTestDatabase.GetConnectionString();
    }
}
