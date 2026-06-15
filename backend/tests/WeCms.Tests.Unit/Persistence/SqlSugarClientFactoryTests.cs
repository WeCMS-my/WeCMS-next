using SqlSugar;
using WeCms.Persistence.Data;

namespace WeCms.Tests.Unit.Persistence;

public sealed class SqlSugarClientFactoryTests
{
    [Fact]
    public void CreateClient_ShouldConfigureMySqlProvider_WhenOptionsAreValid()
    {
        var options = new MySqlPersistenceOptions("Server=localhost;Database=wecms_next;");
        var factory = new SqlSugarClientFactory(options);

        using var client = factory.CreateClient();

        Assert.Equal(DbType.MySql, client.CurrentConnectionConfig.DbType);
        Assert.Equal(options.ConnectionString, client.CurrentConnectionConfig.ConnectionString);
        Assert.False(client.CurrentConnectionConfig.IsAutoCloseConnection);
    }
}
