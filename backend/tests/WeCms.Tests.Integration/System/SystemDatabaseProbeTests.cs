using Microsoft.Extensions.Logging.Abstractions;
using MySqlConnector;
using WeCms.Data.SqlSugar;
using WeCms.Modules.Platform.SqlSugar.System;
using WeCms.Tests.Integration;

namespace WeCms.Tests.Integration.SystemApi;

[Collection(nameof(SharedMySqlCollection))]
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
        using var db = new SqlSugarClientFactory(MissingDatabaseConnectionString()).Create();
        var probe = new SystemDatabaseProbe(db, NullLogger<SystemDatabaseProbe>.Instance);

        var result = await probe.CheckAsync(CancellationToken.None);

        Assert.False(result.Available);
        Assert.Equal("database_unavailable", result.FailureCode);
    }

    private static string RequiredConnectionString()
    {
        return IntegrationTestDatabase.GetConnectionString();
    }

    private static string MissingDatabaseConnectionString()
    {
        var builder = new MySqlConnectionStringBuilder(RequiredConnectionString())
        {
            Database = "wecms_missing_db"
        };

        return builder.ConnectionString;
    }
}
