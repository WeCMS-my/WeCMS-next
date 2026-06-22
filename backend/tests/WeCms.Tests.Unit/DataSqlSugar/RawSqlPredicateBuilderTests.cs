using WeCms.Data.SqlSugar;
using Xunit;

namespace WeCms.Tests.Unit.DataSqlSugar;

public sealed class RawSqlPredicateBuilderTests
{
    [Fact]
    public void TenantSqlPredicateBuilder_BuildsAliasQualifiedTenantPredicate()
    {
        var predicate = TenantSqlPredicateBuilder.Build("u", new QueryFilterContext(42, []));

        Assert.Equal("u.tenant_id = @tenantId", predicate.Sql);
        var parameter = Assert.Single(predicate.Parameters);
        Assert.Equal("@tenantId", parameter.ParameterName);
        Assert.Equal(42L, parameter.Value);
    }

    [Fact]
    public void DataScopeSqlPredicateBuilder_BuildsAliasQualifiedPredicateForConfiguredColumn()
    {
        var predicate = DataScopeSqlPredicateBuilder.Build(
            "l",
            "owner_user_id",
            new QueryFilterContext(null, [100, 200]));

        Assert.Equal("l.owner_user_id IN @dataScopeUserIds", predicate.Sql);
        var parameter = Assert.Single(predicate.Parameters);
        Assert.Equal("@dataScopeUserIds", parameter.ParameterName);
        Assert.Equal(new long[] { 100, 200 }, Assert.IsType<long[]>(parameter.Value));
    }

    [Fact]
    public void SoftDeleteSqlPredicateBuilder_BuildsAliasQualifiedDeletedAtPredicate()
    {
        var predicate = SoftDeleteSqlPredicateBuilder.Build("r");

        Assert.Equal("r.deleted_at IS NULL", predicate.Sql);
        Assert.Empty(predicate.Parameters);
    }

    [Fact]
    public void PredicateBuilders_RejectMissingRequiredValues()
    {
        Assert.Throws<ArgumentException>(() => TenantSqlPredicateBuilder.Build(" ", new QueryFilterContext(42, [])));
        Assert.Throws<InvalidOperationException>(() => TenantSqlPredicateBuilder.Build("u", new QueryFilterContext(null, [])));
        Assert.Throws<ArgumentException>(() => DataScopeSqlPredicateBuilder.Build("u", " ", new QueryFilterContext(null, [1])));
        Assert.Throws<InvalidOperationException>(() => DataScopeSqlPredicateBuilder.Build("u", "created_by_user_id", new QueryFilterContext(null, [])));
    }
}
