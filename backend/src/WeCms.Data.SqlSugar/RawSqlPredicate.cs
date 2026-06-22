using SqlSugar;

namespace WeCms.Data.SqlSugar;

public sealed record RawSqlPredicate(string Sql, IReadOnlyList<SugarParameter> Parameters);
