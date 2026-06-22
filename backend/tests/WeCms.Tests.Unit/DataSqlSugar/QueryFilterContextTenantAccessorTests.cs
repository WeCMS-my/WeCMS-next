using WeCms.Data.SqlSugar;
using WeCms.Shared.Data;

namespace WeCms.Tests.Unit.DataSqlSugar;

public sealed class QueryFilterContextTenantAccessorTests
{
    [Fact]
    public void GetCurrentTenantId_UsesConfiguredTenantId()
    {
        var scoped = new StaticQueryFilterContextAccessor(new QueryFilterContext(42, [99]));
        var accessor = new QueryFilterContextTenantAccessor(scoped);

        Assert.Equal("42", accessor.GetCurrentTenantId());
    }

    [Fact]
    public void GetCurrentTenantId_FallsBackToGlobalWhenMissing()
    {
        var scoped = new StaticQueryFilterContextAccessor(QueryFilterContext.Empty);
        var accessor = new QueryFilterContextTenantAccessor(scoped);

        Assert.Equal("global", accessor.GetCurrentTenantId());
    }

    private sealed class StaticQueryFilterContextAccessor(QueryFilterContext current) : IQueryFilterContextAccessor
    {
        public QueryFilterContext Current { get; } = current;
    }
}
