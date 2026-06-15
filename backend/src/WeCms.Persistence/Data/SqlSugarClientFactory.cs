using SqlSugar;

namespace WeCms.Persistence.Data;

public sealed class SqlSugarClientFactory(MySqlPersistenceOptions options) : ISqlSugarClientFactory
{
    private readonly MySqlPersistenceOptions _options = options.Validate();

    public ISqlSugarClient CreateClient()
    {
        return new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = _options.ConnectionString,
            DbType = DbType.MySql,
            IsAutoCloseConnection = false,
            InitKeyType = InitKeyType.Attribute
        });
    }
}
