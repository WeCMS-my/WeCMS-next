using SqlSugar;

namespace WeCms.Persistence.Data;

public sealed class SqlSugarClientFactory : ISqlSugarClientFactory
{
    private readonly string _connectionString;

    public SqlSugarClientFactory(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new PersistenceConfigurationException("ConnectionStrings:Default is required for WeCMS persistence.");
        }

        _connectionString = connectionString;
    }

    public ISqlSugarClient Create()
    {
        return new SqlSugarScope(new ConnectionConfig
        {
            ConnectionString = _connectionString,
            DbType = DbType.MySql,
            InitKeyType = InitKeyType.Attribute,
            IsAutoCloseConnection = true
        });
    }
}
