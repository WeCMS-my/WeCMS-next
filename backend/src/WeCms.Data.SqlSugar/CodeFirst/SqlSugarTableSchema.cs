namespace WeCms.Data.SqlSugar;

public sealed record SqlSugarTableSchema(
    string TableName,
    IReadOnlyCollection<SqlSugarColumnSchema> Columns,
    IReadOnlyCollection<SqlSugarIndexSchema> Indexes);

public sealed record SqlSugarColumnSchema(string ColumnName, bool IsNullable, int? MaxLength);

public sealed record SqlSugarIndexSchema(string IndexName, bool IsUnique);
