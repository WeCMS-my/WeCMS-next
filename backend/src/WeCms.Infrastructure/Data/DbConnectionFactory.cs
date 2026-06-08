using WeCms.Shared.Contracts;
using System.Data.Common;
using MySqlConnector;

namespace WeCms.Infrastructure.Data;

public sealed class DbConnectionFactory(string connectionString) : IDbConnectionFactory
{
    public async Task<DbConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}