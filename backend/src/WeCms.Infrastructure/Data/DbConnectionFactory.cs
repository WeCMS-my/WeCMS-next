using WeCms.Shared.Contracts;
using System.Data.Common;
using MySqlConnector;

namespace WeCms.Infrastructure.Data;

public sealed class DbConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public DbConnectionFactory(string connectionString)
    {
        var builder = new MySqlConnectionStringBuilder(connectionString)
        {
            Pooling = true,
            MaximumPoolSize = 100,
            MinimumPoolSize = 0,
            ConnectionLifeTime = 300
        };
        _connectionString = builder.ConnectionString;
    }

    public async Task<DbConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}