using SqlSugar;

namespace WeCms.Data.SqlSugar;

public static class DataScopeSqlPredicateBuilder
{
    public static RawSqlPredicate Build(
        string tableAlias,
        string userIdColumn,
        QueryFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.DataScopeUserIds.Count == 0)
        {
            throw new InvalidOperationException("Data scope predicate requires at least one scoped user id.");
        }

        var alias = SqlIdentifier.Require(tableAlias, nameof(tableAlias));
        var column = SqlIdentifier.Require(userIdColumn, nameof(userIdColumn));
        return new RawSqlPredicate(
            $"{alias}.{column} IN @dataScopeUserIds",
            [new SugarParameter("@dataScopeUserIds", context.DataScopeUserIds.ToArray())]);
    }
}
