using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WeCms.Data.SqlSugar;

namespace WeCms.Tests.Unit.DataSqlSugar;

public sealed class SqlSugarDataServiceCollectionExtensionsTests
{
    [Fact]
    public void AddWeCmsSqlSugarData_UsesConfiguredDatabaseConnections_WhenPresent()
    {
        var configuration = Configuration(new Dictionary<string, string?>
        {
            ["Database:DefaultConnection"] = "main",
            ["Database:Connections:0:Name"] = "main",
            ["Database:Connections:0:DbType"] = "MySql",
            ["Database:Connections:0:ConnectionStringName"] = "Default",
            ["Database:Connections:0:Role"] = "Main",
            ["Database:Connections:1:Name"] = "audit",
            ["Database:Connections:1:DbType"] = "MySql",
            ["Database:Connections:1:ConnectionStringName"] = "Audit",
            ["Database:Connections:1:Role"] = "Audit",
            ["ConnectionStrings:Default"] = "server=localhost;database=main;",
            ["ConnectionStrings:Audit"] = "server=localhost;database=audit;"
        });

        var services = new ServiceCollection();
        services.AddWeCmsSqlSugarData(configuration);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<DatabasePlatformOptions>();

        Assert.Equal(["main", "audit"], options.Connections.Select(connection => connection.Name));
    }

    [Fact]
    public void AddWeCmsSqlSugarData_RegistersProductionSqlAuditDefaults()
    {
        var configuration = Configuration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = "server=localhost;database=main;"
        });

        var services = new ServiceCollection();
        services.AddWeCmsSqlSugarData(configuration);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<SqlAuditOptions>();
        var registrars = scope.ServiceProvider.GetServices<ISqlSugarAuditRegistrar>().ToArray();

        Assert.False(options.CaptureAllSql);
        Assert.Equal(SqlAuditOptions.DefaultSlowSqlThresholdMilliseconds, options.SlowSqlThresholdMilliseconds);
        Assert.Contains(registrars, registrar => registrar is SqlSugarSqlAuditRegistrar);
    }

    [Fact]
    public void AddWeCmsSqlSugarData_UsesLegacyDefaultConnection_WhenDatabaseConnectionsAreMissing()
    {
        var configuration = Configuration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = "server=localhost;database=main;"
        });

        var services = new ServiceCollection();
        services.AddWeCmsSqlSugarData(configuration);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<DatabasePlatformOptions>();

        var connection = Assert.Single(options.Connections);
        Assert.Equal("main", connection.Name);
        Assert.Equal("Default", connection.ConnectionStringName);
    }

    private static IConfiguration Configuration(IReadOnlyDictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
