using System.Collections.ObjectModel;

namespace WeCms.Data.SqlSugar;

public sealed record TenantDatabaseOptions(
    TenantDatabaseMode Mode,
    IReadOnlyDictionary<long, string> DedicatedConnections)
{
    public static TenantDatabaseOptions Shared { get; } = new(
        TenantDatabaseMode.Shared,
        new ReadOnlyDictionary<long, string>(new Dictionary<long, string>()));
}
