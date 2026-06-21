using SqlSugar;
using WeCms.Data.SqlSugar;

namespace WeCms.Tests.Unit.DataSqlSugar;

public sealed class SqlSugarClientFactoryTests
{
    [Fact]
    public void Constructor_ThrowsWhenConnectionStringIsMissing()
    {
        var exception = Assert.Throws<DatabaseConfigurationException>(
            () => new SqlSugarClientFactory(" "));

        Assert.Contains("ConnectionStrings:Default", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SqlSugarClientFactory_CreatesDefaultClient()
    {
        var factory = new SqlSugarClientFactory(Options(
            Connection("main", DatabaseConnectionRole.Main, "server=localhost;database=main;", 45),
            Connection("audit", DatabaseConnectionRole.Audit, "server=localhost;database=audit;", 30)));

        using var client = factory.Create();

        Assert.NotNull(client);
        Assert.Equal(45, client.Ado.CommandTimeOut);
    }

    [Fact]
    public void SqlSugarClientFactory_CreatesNamedClient()
    {
        var factory = new SqlSugarClientFactory(Options(
            Connection("main", DatabaseConnectionRole.Main, "server=localhost;database=main;", 45),
            Connection("audit", DatabaseConnectionRole.Audit, "server=localhost;database=audit;", 31)));

        using var client = factory.Create("audit");

        Assert.NotNull(client);
        Assert.Equal(31, client.Ado.CommandTimeOut);
    }

    [Fact]
    public void SqlSugarClientFactory_RegistersAuditHooks()
    {
        var auditRegistrar = new RecordingSqlSugarAuditRegistrar();
        var factory = new SqlSugarClientFactory(
            Options(Connection("main", DatabaseConnectionRole.Main, "server=localhost;database=main;", 45)),
            queryFilterRegistrars: [],
            auditRegistrars: [auditRegistrar]);

        using var client = factory.Create();

        Assert.NotNull(client);
        Assert.Equal(1, auditRegistrar.RegisterCount);
    }

    [Fact]
    public void SqlSugarClientFactory_RegistersQueryFilterHooks()
    {
        var queryFilterRegistrar = new RecordingSqlSugarQueryFilterRegistrar();
        var factory = new SqlSugarClientFactory(
            Options(Connection("main", DatabaseConnectionRole.Main, "server=localhost;database=main;", 45)),
            queryFilterRegistrars: [queryFilterRegistrar],
            auditRegistrars: []);

        using var client = factory.Create();

        Assert.NotNull(client);
        Assert.Equal(1, queryFilterRegistrar.RegisterCount);
    }

    [Fact]
    public void CreateForTenant_UsesSharedDefaultConnectionByDefault()
    {
        var factory = new SqlSugarClientFactory(
            Options(Connection("main", DatabaseConnectionRole.Main, "server=localhost;database=main;", 45)));

        using var client = factory.CreateForTenant(1001);

        Assert.NotNull(client);
        Assert.Equal(45, client.Ado.CommandTimeOut);
    }

    [Fact]
    public void CreateForTenant_UsesDedicatedTenantConnection_WhenConfigured()
    {
        var factory = new SqlSugarClientFactory(
            Options(
                new TenantDatabaseOptions(
                    TenantDatabaseMode.Dedicated,
                    new Dictionary<long, string> { [1001] = "tenant-1001" }),
                Connection("main", DatabaseConnectionRole.Main, "server=localhost;database=main;", 45),
                Connection("tenant-1001", DatabaseConnectionRole.Tenant, "server=localhost;database=tenant_1001;", 61)));

        using var client = factory.CreateForTenant(1001);

        Assert.NotNull(client);
        Assert.Equal(61, client.Ado.CommandTimeOut);
    }

    [Fact]
    public void CreateForTenant_FailsFast_WhenDedicatedTenantIsNotConfigured()
    {
        var factory = new SqlSugarClientFactory(
            Options(
                new TenantDatabaseOptions(TenantDatabaseMode.Dedicated, new Dictionary<long, string>()),
                Connection("main", DatabaseConnectionRole.Main, "server=localhost;database=main;", 45)));

        var exception = Assert.Throws<DatabaseConfigurationException>(() => factory.CreateForTenant(1001));

        Assert.Equal("Tenant 1001 does not have an explicit dedicated database connection configured.", exception.Message);
    }

    private static DatabasePlatformOptions Options(params DatabaseConnectionOptions[] connections)
    {
        return new DatabasePlatformOptions("main", connections);
    }

    private static DatabasePlatformOptions Options(
        TenantDatabaseOptions tenant,
        params DatabaseConnectionOptions[] connections)
    {
        return new DatabasePlatformOptions("main", connections, tenant);
    }

    private static DatabaseConnectionOptions Connection(
        string name,
        DatabaseConnectionRole role,
        string connectionString,
        int timeout)
    {
        return new DatabaseConnectionOptions(
            name,
            DbType.MySql,
            "Default",
            connectionString,
            role,
            true,
            timeout);
    }

    private sealed class RecordingSqlSugarAuditRegistrar : ISqlSugarAuditRegistrar
    {
        public int RegisterCount { get; private set; }

        public void Register(SqlSugarScopeProvider db)
        {
            Assert.NotNull(db);
            RegisterCount++;
        }
    }

    private sealed class RecordingSqlSugarQueryFilterRegistrar : ISqlSugarQueryFilterRegistrar
    {
        public int RegisterCount { get; private set; }

        public void Register(SqlSugarScopeProvider db)
        {
            Assert.NotNull(db);
            RegisterCount++;
        }
    }
}
