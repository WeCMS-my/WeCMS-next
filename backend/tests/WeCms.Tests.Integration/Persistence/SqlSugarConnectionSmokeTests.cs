using WeCms.Persistence.Data;
using WeCms.Tests.Integration;

namespace WeCms.Tests.Integration.Persistence;

[Collection(nameof(SharedMySqlCollection))]
public sealed class SqlSugarConnectionSmokeTests
{

    [DbFact]
    public Task SqlSugarClient_CanConnectToMySql()
    {
        var connectionString = IntegrationTestDatabase.GetConnectionString();

        var factory = new SqlSugarClientFactory(connectionString);

        using var client = factory.Create();
        var canConnect = client.Ado.IsValidConnection();

        Assert.True(canConnect);
        return Task.CompletedTask;
    }
}
