using Microsoft.Extensions.Configuration;
using WeCms.Persistence.Data;

namespace WeCms.Tests.Unit.Infrastructure.Data;

public sealed class DbConnectionFactoryTests
{
    [Fact]
    public void Constructor_ShouldReadConnectionString_FromConfiguration()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Server=localhost;Database=test;"
            })
            .Build();

        var factory = new DbConnectionFactory(config);
        Assert.NotNull(factory); // Constructor should not throw
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenConnectionStringMissing()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var ex = Assert.Throws<InvalidOperationException>(() => new DbConnectionFactory(config));
        Assert.Contains("ConnectionStrings:Default", ex.Message);
    }
}
