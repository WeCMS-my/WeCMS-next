using SqlSugar;

namespace WeCms.Data.SqlSugar;

public sealed record DatabaseConnectionOptions(
    string Name,
    DbType DbType,
    string ConnectionStringName,
    string ConnectionString,
    DatabaseConnectionRole Role,
    bool Enabled,
    int CommandTimeoutSeconds);
