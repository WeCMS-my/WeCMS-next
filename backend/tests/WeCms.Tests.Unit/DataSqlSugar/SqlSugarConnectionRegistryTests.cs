using SqlSugar;
using WeCms.Data.SqlSugar;

namespace WeCms.Tests.Unit.DataSqlSugar;

public sealed class SqlSugarConnectionRegistryTests
{
    [Fact]
    public void SqlSugarConnectionRegistry_ResolvesDefaultConnection()
    {
        var registry = new SqlSugarConnectionRegistry(Options(
            DefaultConnection("main", DatabaseConnectionRole.Main),
            EnabledConnection("log", DatabaseConnectionRole.Log)));

        var config = registry.GetDefaultConnection();

        Assert.Equal("main", config.ConfigId);
        Assert.Equal(DbType.MySql, config.DbType);
        Assert.Equal("server=localhost;database=main;", config.ConnectionString);
        Assert.True(config.IsAutoCloseConnection);
        Assert.Equal(InitKeyType.Attribute, config.InitKeyType);
    }

    [Fact]
    public void SqlSugarConnectionRegistry_ResolvesNamedConnection()
    {
        var registry = new SqlSugarConnectionRegistry(Options(
            DefaultConnection("main", DatabaseConnectionRole.Main),
            EnabledConnection("audit", DatabaseConnectionRole.Audit)));

        var config = registry.GetConnection("audit");

        Assert.Equal("audit", config.ConfigId);
        Assert.Equal("server=localhost;database=audit;", config.ConnectionString);
    }

    [Fact]
    public void SqlSugarConnectionRegistry_ResolvesConnectionsByRole()
    {
        var registry = new SqlSugarConnectionRegistry(Options(
            DefaultConnection("main", DatabaseConnectionRole.Main),
            EnabledConnection("log", DatabaseConnectionRole.Log),
            EnabledConnection("audit", DatabaseConnectionRole.Audit),
            EnabledConnection("file", DatabaseConnectionRole.File),
            EnabledConnection("tenant-1001", DatabaseConnectionRole.Tenant)));

        Assert.Equal("main", Assert.Single(registry.GetConnectionOptionsByRole(DatabaseConnectionRole.Main)).Name);
        Assert.Equal("log", Assert.Single(registry.GetConnectionOptionsByRole(DatabaseConnectionRole.Log)).Name);
        Assert.Equal("audit", Assert.Single(registry.GetConnectionOptionsByRole(DatabaseConnectionRole.Audit)).Name);
        Assert.Equal("file", Assert.Single(registry.GetConnectionOptionsByRole(DatabaseConnectionRole.File)).Name);
        Assert.Equal(["tenant-1001"], registry.GetConnectionOptionsByRole(DatabaseConnectionRole.Tenant).Select(connection => connection.Name));
    }

    [Fact]
    public void SqlSugarConnectionRegistry_Fails_WhenConnectionDisabled()
    {
        var registry = new SqlSugarConnectionRegistry(Options(
            DefaultConnection("main", DatabaseConnectionRole.Main),
            DisabledConnection("audit", DatabaseConnectionRole.Audit)));

        void Act() => registry.GetConnection("audit");

        var exception = Assert.Throws<DatabaseConfigurationException>((Action)Act);

        Assert.Equal("Database connection 'audit' is disabled or not configured.", exception.Message);
    }

    private static DatabasePlatformOptions Options(params DatabaseConnectionOptions[] connections)
    {
        return new DatabasePlatformOptions("main", connections);
    }

    private static DatabaseConnectionOptions DefaultConnection(string name, DatabaseConnectionRole role)
    {
        return Connection(name, role, enabled: true, timeout: 45);
    }

    private static DatabaseConnectionOptions EnabledConnection(string name, DatabaseConnectionRole role)
    {
        return Connection(name, role, enabled: true, timeout: 30);
    }

    private static DatabaseConnectionOptions DisabledConnection(string name, DatabaseConnectionRole role)
    {
        return Connection(name, role, enabled: false, timeout: 30);
    }

    private static DatabaseConnectionOptions Connection(
        string name,
        DatabaseConnectionRole role,
        bool enabled,
        int timeout)
    {
        return new DatabaseConnectionOptions(
            name,
            DbType.MySql,
            "Default",
            $"server=localhost;database={name};",
            role,
            enabled,
            timeout);
    }
}
