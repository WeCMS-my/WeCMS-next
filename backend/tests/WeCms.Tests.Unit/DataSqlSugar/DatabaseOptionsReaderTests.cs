using Microsoft.Extensions.Configuration;
using SqlSugar;
using WeCms.Data.SqlSugar;

namespace WeCms.Tests.Unit.DataSqlSugar;

public sealed class DatabaseOptionsReaderTests
{
    [Fact]
    public void DatabaseOptionsReader_ReadsSingleConnection()
    {
        var options = new DatabaseOptionsReader().Read(Configuration(new Dictionary<string, string?>
        {
            ["Database:DefaultConnection"] = "main",
            ["Database:CommandTimeoutSeconds"] = "45",
            ["Database:Connections:0:Name"] = "main",
            ["Database:Connections:0:DbType"] = "MySql",
            ["Database:Connections:0:ConnectionStringName"] = "Default",
            ["Database:Connections:0:Role"] = "Main",
            ["Database:Connections:0:Enabled"] = "true",
            ["ConnectionStrings:Default"] = "server=localhost;database=wecms;uid=root;pwd=example;"
        }));

        var connection = Assert.Single(options.Connections);
        Assert.Equal("main", options.DefaultConnection);
        Assert.Equal("main", connection.Name);
        Assert.Equal(DbType.MySql, connection.DbType);
        Assert.Equal("Default", connection.ConnectionStringName);
        Assert.Equal("server=localhost;database=wecms;uid=root;pwd=example;", connection.ConnectionString);
        Assert.Equal(DatabaseConnectionRole.Main, connection.Role);
        Assert.True(connection.Enabled);
        Assert.Equal(45, connection.CommandTimeoutSeconds);
        Assert.Equal(TenantDatabaseMode.Shared, options.Tenant.Mode);
        Assert.Empty(options.Tenant.DedicatedConnections);
    }

    [Fact]
    public void DatabaseOptionsReader_ReadsDedicatedTenantConnections_WhenConfigured()
    {
        var options = new DatabaseOptionsReader().Read(Configuration(new Dictionary<string, string?>
        {
            ["Database:DefaultConnection"] = "main",
            ["Database:Tenant:Mode"] = "Dedicated",
            ["Database:Tenant:DedicatedConnections:0:TenantId"] = "1001",
            ["Database:Tenant:DedicatedConnections:0:ConnectionName"] = "tenant-1001",
            ["Database:Connections:0:Name"] = "main",
            ["Database:Connections:0:DbType"] = "MySql",
            ["Database:Connections:0:ConnectionStringName"] = "Default",
            ["Database:Connections:0:Role"] = "Main",
            ["Database:Connections:1:Name"] = "tenant-1001",
            ["Database:Connections:1:DbType"] = "MySql",
            ["Database:Connections:1:ConnectionStringName"] = "Tenant1001",
            ["Database:Connections:1:Role"] = "Tenant",
            ["ConnectionStrings:Default"] = "server=localhost;database=wecms;",
            ["ConnectionStrings:Tenant1001"] = "server=localhost;database=tenant_1001;"
        }));

        Assert.Equal(TenantDatabaseMode.Dedicated, options.Tenant.Mode);
        Assert.Equal("tenant-1001", options.Tenant.DedicatedConnections[1001]);
    }

    [Fact]
    public void DatabaseOptionsReader_Fails_WhenDefaultMissing()
    {
        var exception = Assert.Throws<DatabaseConfigurationException>(
            () => new DatabaseOptionsReader().Read(Configuration(new Dictionary<string, string?>
            {
                ["Database:Connections:0:Name"] = "main",
                ["Database:Connections:0:DbType"] = "MySql",
                ["Database:Connections:0:ConnectionStringName"] = "Default",
                ["Database:Connections:0:Role"] = "Main",
                ["ConnectionStrings:Default"] = "server=localhost;database=wecms;uid=root;pwd=example;"
            })));

        Assert.Equal("Database:DefaultConnection is required.", exception.Message);
    }

    [Fact]
    public void DatabaseOptionsReader_Fails_WhenDuplicateConnectionName()
    {
        var exception = Assert.Throws<DatabaseConfigurationException>(
            () => new DatabaseOptionsReader().Read(Configuration(new Dictionary<string, string?>
            {
                ["Database:DefaultConnection"] = "main",
                ["Database:Connections:0:Name"] = "main",
                ["Database:Connections:0:DbType"] = "MySql",
                ["Database:Connections:0:ConnectionStringName"] = "Default",
                ["Database:Connections:0:Role"] = "Main",
                ["Database:Connections:1:Name"] = "MAIN",
                ["Database:Connections:1:DbType"] = "MySql",
                ["Database:Connections:1:ConnectionStringName"] = "Default",
                ["Database:Connections:1:Role"] = "Log",
                ["ConnectionStrings:Default"] = "server=localhost;database=wecms;uid=root;pwd=example;"
            })));

        Assert.Equal("Database connection name 'MAIN' is duplicated.", exception.Message);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("301")]
    [InlineData("abc")]
    public void DatabaseOptionsReader_Fails_WhenInvalidTimeout(string timeout)
    {
        var exception = Assert.Throws<DatabaseConfigurationException>(
            () => new DatabaseOptionsReader().Read(Configuration(new Dictionary<string, string?>
            {
                ["Database:DefaultConnection"] = "main",
                ["Database:Connections:0:Name"] = "main",
                ["Database:Connections:0:DbType"] = "MySql",
                ["Database:Connections:0:ConnectionStringName"] = "Default",
                ["Database:Connections:0:Role"] = "Main",
                ["Database:Connections:0:CommandTimeoutSeconds"] = timeout,
                ["ConnectionStrings:Default"] = "server=localhost;database=wecms;uid=root;pwd=example;"
            })));

        Assert.Equal("Database:Connections:0:CommandTimeoutSeconds must be an integer between 1 and 300.", exception.Message);
    }

    [Fact]
    public void DatabaseOptionsReader_Fails_WhenInvalidDbType()
    {
        var exception = Assert.Throws<DatabaseConfigurationException>(
            () => new DatabaseOptionsReader().Read(Configuration(new Dictionary<string, string?>
            {
                ["Database:DefaultConnection"] = "main",
                ["Database:Connections:0:Name"] = "main",
                ["Database:Connections:0:DbType"] = "PostgreSql",
                ["Database:Connections:0:ConnectionStringName"] = "Default",
                ["Database:Connections:0:Role"] = "Main",
                ["ConnectionStrings:Default"] = "server=localhost;database=wecms;uid=root;pwd=example;"
            })));

        Assert.Equal("Database:Connections:0:DbType must be one of: MySql.", exception.Message);
    }

    private static IConfiguration Configuration(IReadOnlyDictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
