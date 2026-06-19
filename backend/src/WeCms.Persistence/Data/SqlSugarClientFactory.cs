using SqlSugar;

namespace WeCms.Persistence.Data;

public sealed class SqlSugarClientFactory : ISqlSugarClientFactory
{
    private readonly string _connectionString;
    private readonly DatabaseOptions _options;

    public SqlSugarClientFactory(string connectionString)
        : this(connectionString, new DatabaseOptions(DatabaseOptions.DefaultCommandTimeoutSeconds))
    {
    }

    public SqlSugarClientFactory(string connectionString, DatabaseOptions options)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new PersistenceConfigurationException("ConnectionStrings:Default is required for WeCMS persistence.");
        }

        _connectionString = connectionString;
        _options = options;
    }

    public ISqlSugarClient Create()
    {
        var client = new SqlSugarScope(new ConnectionConfig
        {
            ConnectionString = _connectionString,
            DbType = DbType.MySql,
            InitKeyType = InitKeyType.Attribute,
            IsAutoCloseConnection = true
        });

        client.Ado.CommandTimeOut = _options.CommandTimeoutSeconds;
        return client;
    }
}
