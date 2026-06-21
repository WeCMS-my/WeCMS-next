namespace WeCms.Data.SqlSugar;

public sealed record SqlAuditContext(
    string? TraceId,
    long? UserId,
    string? Username,
    long? TenantId,
    string? RepositoryName)
{
    public static SqlAuditContext Empty { get; } = new(null, null, null, null, null);
}

public interface ISqlAuditContextAccessor
{
    SqlAuditContext Current { get; }
}

public sealed class AmbientSqlAuditContextAccessor : ISqlAuditContextAccessor
{
    private static readonly AsyncLocal<SqlAuditContext?> AmbientCurrent = new();

    public SqlAuditContext Current => AmbientCurrent.Value ?? SqlAuditContext.Empty;

    public IDisposable Push(SqlAuditContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var previous = AmbientCurrent.Value;
        AmbientCurrent.Value = context;
        return new PopWhenDisposed(previous);
    }

    private sealed class PopWhenDisposed(SqlAuditContext? previous) : IDisposable
    {
        public void Dispose()
        {
            AmbientCurrent.Value = previous;
        }
    }
}
