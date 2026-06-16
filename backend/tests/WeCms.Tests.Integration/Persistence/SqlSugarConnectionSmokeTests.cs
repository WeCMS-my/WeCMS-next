using WeCms.Persistence.Data;

namespace WeCms.Tests.Integration.Persistence;

public sealed class SqlSugarConnectionSmokeTests
{
    [Fact]
    public Task SqlSugarClient_CanConnectToMySql()
    {
        var connectionString = Environment.GetEnvironmentVariable("WECMS_TEST_MYSQL_CONNECTION_STRING");
        Assert.False(
            string.IsNullOrWhiteSpace(connectionString),
            "Set WECMS_TEST_MYSQL_CONNECTION_STRING to run the MySQL integration smoke test.");

        var factory = new SqlSugarClientFactory(connectionString);

        using var client = factory.Create();
        var canConnect = client.Ado.IsValidConnection();

        Assert.True(canConnect);
        return Task.CompletedTask;
    }
}
