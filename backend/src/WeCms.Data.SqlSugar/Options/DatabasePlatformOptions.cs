namespace WeCms.Data.SqlSugar;

public sealed record DatabasePlatformOptions(
    string DefaultConnection,
    IReadOnlyList<DatabaseConnectionOptions> Connections,
    TenantDatabaseOptions Tenant)
{
    public DatabasePlatformOptions(
        string DefaultConnection,
        IReadOnlyList<DatabaseConnectionOptions> Connections)
        : this(DefaultConnection, Connections, TenantDatabaseOptions.Shared)
    {
    }

    public const int DefaultCommandTimeoutSeconds = 30;
    public const int MinimumCommandTimeoutSeconds = 1;
    public const int MaximumCommandTimeoutSeconds = 300;
}
