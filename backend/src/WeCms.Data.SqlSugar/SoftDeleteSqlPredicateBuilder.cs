namespace WeCms.Data.SqlSugar;

public static class SoftDeleteSqlPredicateBuilder
{
    public static RawSqlPredicate Build(string tableAlias)
    {
        var alias = SqlIdentifier.Require(tableAlias, nameof(tableAlias));
        return new RawSqlPredicate($"{alias}.deleted_at IS NULL", []);
    }
}
