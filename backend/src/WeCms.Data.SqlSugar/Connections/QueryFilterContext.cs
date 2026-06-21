namespace WeCms.Data.SqlSugar;

public sealed record QueryFilterContext(long? TenantId, IReadOnlyCollection<long> DataScopeUserIds)
{
    public static QueryFilterContext Empty { get; } = new(null, []);
}

public interface IQueryFilterContextAccessor
{
    QueryFilterContext Current { get; }
}

public sealed class AmbientQueryFilterContextAccessor : IQueryFilterContextAccessor
{
    private static readonly AsyncLocal<QueryFilterContext?> CurrentContext = new();

    public QueryFilterContext Current => CurrentContext.Value ?? QueryFilterContext.Empty;

    public IDisposable Push(QueryFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var previous = CurrentContext.Value;
        CurrentContext.Value = context;
        return new Scope(() => CurrentContext.Value = previous);
    }

    private sealed class Scope : IDisposable
    {
        private readonly Action _dispose;
        private bool _disposed;

        public Scope(Action dispose)
        {
            _dispose = dispose;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _dispose();
            _disposed = true;
        }
    }
}
