using SqlSugar;

namespace WeCms.Data.SqlSugar;

public sealed class SqlSugarConnectionRegistry
{
    private readonly DatabasePlatformOptions _options;
    private readonly IReadOnlyDictionary<string, DatabaseConnectionOptions> _enabledConnections;

    public SqlSugarConnectionRegistry(DatabasePlatformOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options;
        _enabledConnections = options.Connections
            .Where(connection => connection.Enabled)
            .ToDictionary(connection => connection.Name, StringComparer.OrdinalIgnoreCase);
    }

    public ConnectionConfig GetDefaultConnection()
    {
        return GetConnection(_options.DefaultConnection);
    }

    public DatabaseConnectionOptions GetDefaultConnectionOptions()
    {
        return GetConnectionOptions(_options.DefaultConnection);
    }

    public ConnectionConfig GetConnection(string connectionName)
    {
        return ToConnectionConfig(GetConnectionOptions(connectionName));
    }

    public DatabaseConnectionOptions GetConnectionOptions(string connectionName)
    {
        if (string.IsNullOrWhiteSpace(connectionName)
            || !_enabledConnections.TryGetValue(connectionName.Trim(), out var connection))
        {
            throw new DatabaseConfigurationException($"Database connection '{connectionName}' is disabled or not configured.");
        }

        return connection;
    }

    public IReadOnlyList<DatabaseConnectionOptions> GetConnectionOptionsByRole(DatabaseConnectionRole role)
    {
        return _enabledConnections
            .Values
            .Where(connection => connection.Role == role)
            .ToArray();
    }

    public IReadOnlyList<ConnectionConfig> GetEnabledConnectionConfigs()
    {
        return _enabledConnections
            .Values
            .Select(ToConnectionConfig)
            .ToArray();
    }

    private static ConnectionConfig ToConnectionConfig(DatabaseConnectionOptions connection)
    {
        return new ConnectionConfig
        {
            ConfigId = connection.Name,
            ConnectionString = connection.ConnectionString,
            DbType = connection.DbType,
            InitKeyType = InitKeyType.Attribute,
            IsAutoCloseConnection = true
        };
    }
}
