using System.Data;
using System.Data.Common;
using System.Text.Json;
using MySqlConnector;
using SqlSugar;
using WeCms.Persistence.Data;
using Xunit;

namespace WeCms.Tests.Integration;

internal static class IntegrationTestDatabase
{
    private const string EnvVarName = "WECMS_TEST_MYSQL_CONNECTION_STRING";
    private const string AllowedHostsEnvVar = "WECMS_TEST_MYSQL_ALLOWED_HOSTS";
    private const string AllowedHost = "192.168.101.199";
    private const string AllowedDatabase = "wecms_dev";
    private static readonly SemaphoreSlim ResetLock = new(1, 1);

    public static string GetConnectionString()
    {
        var connectionString = ResolveConnectionString();

        Assert.False(
            string.IsNullOrWhiteSpace(connectionString),
            "Set WECMS_TEST_MYSQL_CONNECTION_STRING or configure backend/src/WeCms.Api/appsettings.Development.json (ConnectionStrings:Test/Default) to run MySQL integration tests.");

        var validationFailure = GetTestConnectionValidationFailure(connectionString);
        Assert.True(
            string.IsNullOrWhiteSpace(validationFailure),
            validationFailure ??
            $"Integration test database must use development database only. Expected host='{GetAllowedHostsText()}' and database='{AllowedDatabase}'. Use: server={AllowedHost};port=3306;database={AllowedDatabase};uid=wecms_dev;pwd=****;charset=utf8mb4;SslMode=None;.");

        return connectionString!;
    }

    public static bool IsDatabaseAvailable([System.Diagnostics.CodeAnalysis.NotNullWhen(false)] out string? unavailableReason)
    {
        unavailableReason = null;
        var connectionString = ResolveConnectionString();

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            unavailableReason = "Set WECMS_TEST_MYSQL_CONNECTION_STRING or configure backend/src/WeCms.Api/appsettings.Development.json (ConnectionStrings:Test/Default).";
            return false;
        }

        var validationFailure = GetTestConnectionValidationFailure(connectionString);
        if (!string.IsNullOrWhiteSpace(validationFailure))
        {
            unavailableReason = validationFailure;
            return false;
        }

        try
        {
            using var db = new SqlSugarClientFactory(connectionString).Create();
            db.Ado.GetScalar("SELECT 1");
            return true;
        }
        catch (Exception ex)
        {
            unavailableReason = ex.Message;
            return false;
        }
    }

    public static async Task ResetDatabaseAsync(string connectionString)
    {
        var validationFailure = GetTestConnectionValidationFailure(connectionString);
        Assert.True(string.IsNullOrWhiteSpace(validationFailure), validationFailure ?? "Integration test database connection is not allowed.");

        await ResetLock.WaitAsync();
        try
        {
            await using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            await ResetDatabaseAsync(connection);
        }
        finally
        {
            ResetLock.Release();
        }
    }

    public static async Task ResetDatabaseAsync(ISqlSugarClient db)
    {
        await ResetLock.WaitAsync();
        try
        {
            await using var connection = new MySqlConnection(db.Ado.Connection.ConnectionString);
            await connection.OpenAsync();
            await ResetDatabaseAsync(connection);
        }
        finally
        {
            ResetLock.Release();
        }
    }

    private static async Task ResetDatabaseAsync(MySqlConnection connection)
    {
        var tableNames = new List<string>();

        await using (var tablesCommand = connection.CreateCommand())
        {
            tablesCommand.CommandText = """
                SELECT TABLE_NAME
                FROM information_schema.TABLES
                WHERE TABLE_SCHEMA = DATABASE()
                  AND TABLE_TYPE = 'BASE TABLE'
                ORDER BY TABLE_NAME
                """;

            await using var reader = await tablesCommand.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                if (!reader.IsDBNull(0))
                {
                    tableNames.Add(reader.GetString(0));
                }
            }
        }

        if (tableNames.Count == 0)
        {
            return;
        }

        await using var disableCommand = connection.CreateCommand();
        disableCommand.CommandText = "SET SESSION FOREIGN_KEY_CHECKS = 0";
        await disableCommand.ExecuteNonQueryAsync();

        try
        {
            foreach (var table in tableNames)
            {
                await using var dropCommand = connection.CreateCommand();
                dropCommand.CommandText = $"DROP TABLE IF EXISTS {QuoteIdentifier(table)}";
                await dropCommand.ExecuteNonQueryAsync();
            }
        }
        finally
        {
            await using var enableCommand = connection.CreateCommand();
            enableCommand.CommandText = "SET SESSION FOREIGN_KEY_CHECKS = 1";
            await enableCommand.ExecuteNonQueryAsync();
        }
    }

    private static string QuoteIdentifier(string identifier)
    {
        return $"`{identifier.Replace("`", "``", StringComparison.Ordinal)}`";
    }

    private static string? ResolveConnectionString()
    {
        return Environment.GetEnvironmentVariable(EnvVarName)
            ?? ReadConnectionStringFromAppSettings("Test")
            ?? ReadConnectionStringFromAppSettings("Default");
    }

    private static string? GetTestConnectionValidationFailure(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return "Set WECMS_TEST_MYSQL_CONNECTION_STRING or configure backend/src/WeCms.Api/appsettings.Development.json (ConnectionStrings:Test/Default).";
        }

        try
        {
            var builder = new DbConnectionStringBuilder
            {
                ConnectionString = connectionString
            };

            var host = ExtractConnectionStringValue(builder, "server")?.Trim() ??
                       ExtractConnectionStringValue(builder, "data source")?.Trim() ??
                       ExtractConnectionStringValue(builder, "host")?.Trim();

            if (string.IsNullOrWhiteSpace(host))
            {
                return "Connection string must include server (Server).";
            }

            host = NormalizeHost(host);

            if (!GetAllowedHosts().Contains(host, StringComparer.OrdinalIgnoreCase))
            {
                return $"Forbidden test database host: {host}. Integration tests are restricted to development hosts {GetAllowedHostsText()}.";
            }

            var database = ExtractConnectionStringValue(builder, "database")?.Trim() ??
                           ExtractConnectionStringValue(builder, "initial catalog")?.Trim();

            if (string.IsNullOrWhiteSpace(database))
            {
                return "Connection string must include database (Database).";
            }

            if (!string.Equals(database, AllowedDatabase, StringComparison.OrdinalIgnoreCase))
            {
                return $"Forbidden test database '{database}'. Integration tests are restricted to database '{AllowedDatabase}'.";
            }

            return null;
        }
        catch (ArgumentException)
        {
            return "Invalid connection string format.";
        }
    }

    private static string NormalizeHost(string host)
    {
        host = host.Trim().Trim('"', '\'');

        if (host.StartsWith("[") && host.Contains("]", StringComparison.Ordinal))
        {
            var end = host.IndexOf("]", StringComparison.Ordinal);
            host = host[1..end];
        }

        var portIndex = host.IndexOf(":", StringComparison.Ordinal);
        if (portIndex > 0)
        {
            host = host[..portIndex];
        }

        return host;
    }

    private static string? ExtractConnectionStringValue(DbConnectionStringBuilder builder, string key)
    {
        return builder.TryGetValue(key, out var value) && value is not null
            ? value.ToString()
            : null;
    }

    private static string GetAllowedHostsText()
    {
        return string.Join(", ", GetAllowedHosts());
    }

    private static string[] GetAllowedHosts()
    {
        var configured = Environment.GetEnvironmentVariable(AllowedHostsEnvVar);
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(NormalizeHost)
                .Where(host => !string.IsNullOrWhiteSpace(host))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        return [AllowedHost];
    }

    private static string? ReadConnectionStringFromAppSettings(string name)
    {
        var path = RepoPath("backend", "src", "WeCms.Api", "appsettings.Development.json");
        if (!File.Exists(path))
        {
            return null;
        }

        using var stream = File.OpenRead(path);
        using var json = JsonDocument.Parse(stream);
        if (!json.RootElement.TryGetProperty("ConnectionStrings", out var connectionStrings))
        {
            return null;
        }

        return connectionStrings.TryGetProperty(name, out var specificConnectionString)
            ? specificConnectionString.GetString()
            : null;
    }

    private static string RepoPath(params string[] segments)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "backend"))
                && File.Exists(Path.Combine(directory.FullName, "backend", "src", "WeCms.Api", "WeCms.Api.csproj")))
            {
                return Path.Combine([directory.FullName, .. segments]);
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
