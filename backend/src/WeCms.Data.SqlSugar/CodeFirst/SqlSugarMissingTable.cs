namespace WeCms.Data.SqlSugar;

public sealed record SqlSugarMissingTable(Type ModelType, string TableName);
