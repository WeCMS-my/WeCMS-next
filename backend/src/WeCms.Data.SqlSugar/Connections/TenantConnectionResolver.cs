namespace WeCms.Data.SqlSugar;

public sealed class TenantConnectionResolver : ITenantConnectionResolver
{
    private readonly DatabasePlatformOptions _options;
    private readonly SqlSugarConnectionRegistry _registry;

    public TenantConnectionResolver(
        DatabasePlatformOptions options,
        SqlSugarConnectionRegistry registry)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    public DatabaseConnectionOptions Resolve(long tenantId)
    {
        if (tenantId <= 0)
        {
            throw new DatabaseConfigurationException("Tenant id must be a positive integer.");
        }

        if (_options.Tenant.Mode == TenantDatabaseMode.Shared)
        {
            return _registry.GetDefaultConnectionOptions();
        }

        if (!_options.Tenant.DedicatedConnections.TryGetValue(tenantId, out var connectionName))
        {
            throw new DatabaseConfigurationException($"Tenant {tenantId} does not have an explicit dedicated database connection configured.");
        }

        var connection = _registry.GetConnectionOptions(connectionName);
        if (connection.Role != DatabaseConnectionRole.Tenant)
        {
            throw new DatabaseConfigurationException($"Tenant database connection '{connection.Name}' must use the Tenant role.");
        }

        return connection;
    }
}
