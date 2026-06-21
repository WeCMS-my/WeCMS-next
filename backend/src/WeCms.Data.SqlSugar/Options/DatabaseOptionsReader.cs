using System.Collections.ObjectModel;
using System.Globalization;
using Microsoft.Extensions.Configuration;
using SqlSugar;

namespace WeCms.Data.SqlSugar;

public sealed class DatabaseOptionsReader
{
    private static readonly DbType[] SupportedDbTypes = [DbType.MySql];

    public DatabasePlatformOptions Read(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration.GetSection("Database");
        var defaultConnection = RequiredString(section, "DefaultConnection", "Database:DefaultConnection");
        var defaultTimeout = ReadInt(
            section,
            "CommandTimeoutSeconds",
            "Database:CommandTimeoutSeconds",
            DatabasePlatformOptions.DefaultCommandTimeoutSeconds);

        var connections = ReadConnections(configuration, section, defaultTimeout);
        if (connections.Count == 0)
        {
            throw new DatabaseConfigurationException("Database:Connections must contain at least one connection.");
        }

        if (!connections.Any(connection => string.Equals(connection.Name, defaultConnection, StringComparison.OrdinalIgnoreCase)))
        {
            throw new DatabaseConfigurationException($"Database:DefaultConnection '{defaultConnection}' does not match any configured connection.");
        }

        var tenant = ReadTenantOptions(section, connections);

        return new DatabasePlatformOptions(
            defaultConnection,
            new ReadOnlyCollection<DatabaseConnectionOptions>(connections),
            tenant);
    }

    private static TenantDatabaseOptions ReadTenantOptions(
        IConfigurationSection databaseSection,
        IReadOnlyList<DatabaseConnectionOptions> connections)
    {
        var tenantSection = databaseSection.GetSection("Tenant");
        var modeValue = tenantSection["Mode"];
        var mode = string.IsNullOrWhiteSpace(modeValue)
            ? TenantDatabaseMode.Shared
            : ReadTenantDatabaseMode(modeValue, "Database:Tenant:Mode");

        var dedicatedConnections = ReadDedicatedTenantConnections(tenantSection, connections);
        if (mode == TenantDatabaseMode.Shared && dedicatedConnections.Count > 0)
        {
            throw new DatabaseConfigurationException("Database:Tenant:DedicatedConnections requires Database:Tenant:Mode to be Dedicated.");
        }

        return new TenantDatabaseOptions(
            mode,
            new ReadOnlyDictionary<long, string>(dedicatedConnections));
    }

    private static Dictionary<long, string> ReadDedicatedTenantConnections(
        IConfigurationSection tenantSection,
        IReadOnlyList<DatabaseConnectionOptions> connections)
    {
        var dedicatedConnections = new Dictionary<long, string>();
        var connectionLookup = connections.ToDictionary(connection => connection.Name, StringComparer.OrdinalIgnoreCase);
        var dedicatedSections = tenantSection.GetSection("DedicatedConnections").GetChildren().ToArray();

        for (var index = 0; index < dedicatedSections.Length; index++)
        {
            var dedicatedSection = dedicatedSections[index];
            var keyPrefix = $"Database:Tenant:DedicatedConnections:{index}";
            var tenantId = ReadTenantId(dedicatedSection, $"{keyPrefix}:TenantId");
            if (dedicatedConnections.ContainsKey(tenantId))
            {
                throw new DatabaseConfigurationException($"Database tenant connection for tenant '{tenantId}' is duplicated.");
            }

            var connectionName = RequiredString(dedicatedSection, "ConnectionName", $"{keyPrefix}:ConnectionName");
            if (!connectionLookup.TryGetValue(connectionName, out var connection))
            {
                throw new DatabaseConfigurationException($"Tenant database connection '{connectionName}' is not configured.");
            }

            if (!connection.Enabled)
            {
                throw new DatabaseConfigurationException($"Tenant database connection '{connectionName}' is disabled.");
            }

            if (connection.Role != DatabaseConnectionRole.Tenant)
            {
                throw new DatabaseConfigurationException($"Tenant database connection '{connectionName}' must use the Tenant role.");
            }

            dedicatedConnections.Add(tenantId, connection.Name);
        }

        return dedicatedConnections;
    }

    private static TenantDatabaseMode ReadTenantDatabaseMode(string value, string key)
    {
        if (!Enum.TryParse<TenantDatabaseMode>(value, ignoreCase: true, out var mode))
        {
            throw new DatabaseConfigurationException($"{key} must be one of: Shared, Dedicated.");
        }

        return mode;
    }

    private static List<DatabaseConnectionOptions> ReadConnections(
        IConfiguration configuration,
        IConfigurationSection databaseSection,
        int defaultTimeout)
    {
        var connections = new List<DatabaseConnectionOptions>();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var connectionSections = databaseSection.GetSection("Connections").GetChildren().ToArray();

        for (var index = 0; index < connectionSections.Length; index++)
        {
            var connectionSection = connectionSections[index];
            var keyPrefix = $"Database:Connections:{index}";
            var name = RequiredString(connectionSection, "Name", $"{keyPrefix}:Name");
            if (!names.Add(name))
            {
                throw new DatabaseConfigurationException($"Database connection name '{name}' is duplicated.");
            }

            var dbType = ReadDbType(connectionSection, $"{keyPrefix}:DbType");
            var connectionStringName = RequiredString(connectionSection, "ConnectionStringName", $"{keyPrefix}:ConnectionStringName");
            var connectionString = configuration.GetConnectionString(connectionStringName);
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new DatabaseConfigurationException($"ConnectionStrings:{connectionStringName} is required for database connection '{name}'.");
            }

            var role = ReadRole(connectionSection, $"{keyPrefix}:Role");
            var enabled = ReadBool(connectionSection, "Enabled", $"{keyPrefix}:Enabled", defaultValue: true);
            var timeout = ReadInt(connectionSection, "CommandTimeoutSeconds", $"{keyPrefix}:CommandTimeoutSeconds", defaultTimeout);

            connections.Add(new DatabaseConnectionOptions(
                name,
                dbType,
                connectionStringName,
                connectionString,
                role,
                enabled,
                timeout));
        }

        return connections;
    }

    private static DbType ReadDbType(IConfigurationSection section, string key)
    {
        var value = RequiredString(section, "DbType", key);
        if (!Enum.TryParse<DbType>(value, ignoreCase: true, out var dbType)
            || !SupportedDbTypes.Contains(dbType))
        {
            throw new DatabaseConfigurationException($"{key} must be one of: MySql.");
        }

        return dbType;
    }

    private static DatabaseConnectionRole ReadRole(IConfigurationSection section, string key)
    {
        var value = RequiredString(section, "Role", key);
        if (!Enum.TryParse<DatabaseConnectionRole>(value, ignoreCase: true, out var role))
        {
            throw new DatabaseConfigurationException($"{key} must be one of: Main, Log, Audit, File, Tenant.");
        }

        return role;
    }

    private static long ReadTenantId(IConfigurationSection section, string key)
    {
        var value = RequiredString(section, "TenantId", key);
        if (!long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed)
            || parsed <= 0)
        {
            throw new DatabaseConfigurationException($"{key} must be a positive integer.");
        }

        return parsed;
    }

    private static bool ReadBool(
        IConfigurationSection section,
        string relativeKey,
        string fullKey,
        bool defaultValue)
    {
        var value = section[relativeKey];
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        if (!bool.TryParse(value, out var parsed))
        {
            throw new DatabaseConfigurationException($"{fullKey} must be true or false.");
        }

        return parsed;
    }

    private static int ReadInt(
        IConfigurationSection section,
        string relativeKey,
        string fullKey,
        int defaultValue)
    {
        var value = section[relativeKey];
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed)
            || parsed < DatabasePlatformOptions.MinimumCommandTimeoutSeconds
            || parsed > DatabasePlatformOptions.MaximumCommandTimeoutSeconds)
        {
            throw new DatabaseConfigurationException(
                $"{fullKey} must be an integer between {DatabasePlatformOptions.MinimumCommandTimeoutSeconds} and {DatabasePlatformOptions.MaximumCommandTimeoutSeconds}.");
        }

        return parsed;
    }

    private static string RequiredString(IConfigurationSection section, string relativeKey, string fullKey)
    {
        var value = section[relativeKey];
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DatabaseConfigurationException($"{fullKey} is required.");
        }

        return value.Trim();
    }
}
