namespace WeCms.Data.SqlSugar;

public sealed record SqlSugarMissingColumn(string TableName, string ColumnName);

public sealed record SqlSugarNullableMismatch(
    string TableName,
    string ColumnName,
    bool ExpectedNullable,
    bool ActualNullable);

public sealed record SqlSugarLengthMismatch(
    string TableName,
    string ColumnName,
    int ExpectedMaxLength,
    int? ActualMaxLength);

public sealed record SqlSugarIndexMismatch(string TableName, string IndexName, string Reason);
