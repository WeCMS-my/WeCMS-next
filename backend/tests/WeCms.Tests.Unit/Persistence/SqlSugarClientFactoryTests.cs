using WeCms.Persistence.Data;

namespace WeCms.Tests.Unit.Persistence;

public sealed class SqlSugarClientFactoryTests
{
    [Fact]
    public void Constructor_ThrowsWhenConnectionStringIsMissing()
    {
        var exception = Assert.Throws<PersistenceConfigurationException>(
            () => new SqlSugarClientFactory(" "));

        Assert.Contains("ConnectionStrings:Default", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_ReturnsSqlSugarClientWithoutOpeningConnection()
    {
        var factory = new SqlSugarClientFactory("server=localhost;port=3306;database=wecms;uid=root;pwd=example;");

        using var client = factory.Create();

        Assert.NotNull(client);
    }
}
