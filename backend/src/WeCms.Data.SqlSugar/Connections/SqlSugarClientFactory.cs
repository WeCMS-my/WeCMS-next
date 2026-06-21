using SqlSugar;

namespace WeCms.Data.SqlSugar;

public sealed class SqlSugarClientFactory : ISqlSugarClientFactory
{
    private const string DefaultLegacyConnectionName = "main";
    private readonly DatabasePlatformOptions _options;
    private readonly SqlSugarConnectionRegistry _registry;
    private readonly ITenantConnectionResolver _tenantConnectionResolver;
    private readonly IReadOnlyList<ISqlSugarQueryFilterRegistrar> _queryFilterRegistrars;
    private readonly IReadOnlyList<ISqlSugarAuditRegistrar> _auditRegistrars;

    public SqlSugarClientFactory(string connectionString)
        : this(connectionString, DatabasePlatformOptions.DefaultCommandTimeoutSeconds)
    {
    }

    public SqlSugarClientFactory(string connectionString, int commandTimeoutSeconds)
        : this(connectionString, commandTimeoutSeconds, queryFilterRegistrars: [], auditRegistrars: [])
    {
    }

    public SqlSugarClientFactory(
        string connectionString,
        int commandTimeoutSeconds,
        IEnumerable<ISqlSugarQueryFilterRegistrar> queryFilterRegistrars,
        IEnumerable<ISqlSugarAuditRegistrar> auditRegistrars)
        : this(LegacyOptions(connectionString, commandTimeoutSeconds), queryFilterRegistrars, auditRegistrars)
    {
    }

    public SqlSugarClientFactory(
        DatabasePlatformOptions options,
        IEnumerable<ISqlSugarQueryFilterRegistrar>? queryFilterRegistrars = null,
        IEnumerable<ISqlSugarAuditRegistrar>? auditRegistrars = null,
        ITenantConnectionResolver? tenantConnectionResolver = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options;
        _registry = new SqlSugarConnectionRegistry(options);
        _tenantConnectionResolver = tenantConnectionResolver ?? new TenantConnectionResolver(options, _registry);
        _queryFilterRegistrars = (queryFilterRegistrars ?? []).ToArray();
        _auditRegistrars = (auditRegistrars ?? []).ToArray();
    }

    public ISqlSugarClient Create()
    {
        return Create(_options.DefaultConnection);
    }

    public ISqlSugarClient Create(string connectionName)
    {
        var connection = _registry.GetConnectionOptions(connectionName);
        var client = new SqlSugarScope(_registry.GetEnabledConnectionConfigs().ToList());
        client.ChangeDatabase(connection.Name);

        var provider = client.GetConnectionScope(connection.Name);
        provider.Ado.CommandTimeOut = connection.CommandTimeoutSeconds;
        RegisterHooks(provider);

        return client;
    }

    public ISqlSugarClient CreateForTenant(long tenantId)
    {
        var connection = _tenantConnectionResolver.Resolve(tenantId);
        return Create(connection.Name);
    }

    private void RegisterHooks(SqlSugarScopeProvider provider)
    {
        foreach (var registrar in _queryFilterRegistrars)
        {
            registrar.Register(provider);
        }

        foreach (var registrar in _auditRegistrars)
        {
            registrar.Register(provider);
        }
    }

    private static DatabasePlatformOptions LegacyOptions(string connectionString, int commandTimeoutSeconds)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new DatabaseConfigurationException("ConnectionStrings:Default is required for WeCMS persistence.");
        }

        if (commandTimeoutSeconds is < DatabasePlatformOptions.MinimumCommandTimeoutSeconds or > DatabasePlatformOptions.MaximumCommandTimeoutSeconds)
        {
            throw new DatabaseConfigurationException(
                $"Database:CommandTimeoutSeconds must be an integer between {DatabasePlatformOptions.MinimumCommandTimeoutSeconds} and {DatabasePlatformOptions.MaximumCommandTimeoutSeconds}.");
        }

        return new DatabasePlatformOptions(
            DefaultLegacyConnectionName,
            [
                new DatabaseConnectionOptions(
                    DefaultLegacyConnectionName,
                    DbType.MySql,
                    "Default",
                    connectionString,
                    DatabaseConnectionRole.Main,
                    true,
                    commandTimeoutSeconds)
            ]);
    }
}
