using SqlSugar;

namespace WeCms.Data.SqlSugar;

public static class TenantSqlPredicateBuilder
{
    public static RawSqlPredicate Build(string tableAlias, QueryFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.TenantId is null)
        {
            throw new InvalidOperationException("Tenant predicate requires a current tenant id.");
        }

        var alias = SqlIdentifier.Require(tableAlias, nameof(tableAlias));
        return new RawSqlPredicate(
            $"{alias}.tenant_id = @tenantId",
            [new SugarParameter("@tenantId", context.TenantId.Value)]);
    }
}
