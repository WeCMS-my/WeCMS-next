using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WeCms.Persistence;
using WeCms.Persistence.Data;
using WeCms.Shared.Data;

namespace WeCms.Tests.Unit.Persistence;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddWeCmsPersistence_ShouldRegisterPersistenceServices_WhenConnectionStringExists()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:WeCms"] = "Server=localhost;Database=wecms_next;"
            })
            .Build();
        var services = new ServiceCollection();

        services.AddWeCmsPersistence(configuration);

        using var provider = services.BuildServiceProvider();
        Assert.IsType<SqlSugarClientFactory>(provider.GetRequiredService<ISqlSugarClientFactory>());
        Assert.IsType<SqlSugarUnitOfWork>(provider.GetRequiredService<IUnitOfWork>());
    }
}
