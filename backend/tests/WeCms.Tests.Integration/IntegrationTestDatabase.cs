using System.Data;
using System.Data.Common;
using System.Text.Json;
using SqlSugar;
using WeCms.Persistence.Data;
using Xunit;

namespace WeCms.Tests.Integration;

internal static class IntegrationTestDatabase
{
    private const string EnvVarName = "WECMS_TEST_MYSQL_CONNECTION_STRING";
    private const string AllowedHost = "192.168.101.199";
    private const string AllowedDatabase = "wecms_dev";

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
            $"Integration test database must use development database only. Expected host='{AllowedHost}' and database='{AllowedDatabase}'. Use: server={AllowedHost};port=3306;database={AllowedDatabase};uid=wecms_dev;pwd=****;charset=utf8mb4;SslMode=None;.");

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

        using var db = new SqlSugarClientFactory(connectionString).Create();
        await ResetDatabaseAsync(db);
    }

    public static async Task ResetDatabaseAsync(ISqlSugarClient db)
    {
        var tables = db.Ado.GetDataTable(
            "SELECT TABLE_NAME FROM information_schema.TABLES WHERE TABLE_SCHEMA = DATABASE() AND TABLE_TYPE = 'BASE TABLE' ORDER BY TABLE_NAME");
        var foreignKeys = db.Ado.GetDataTable(
            """
            SELECT
                TABLE_NAME AS table_name,
                REFERENCED_TABLE_NAME AS referenced_table_name
            FROM information_schema.KEY_COLUMN_USAGE
            WHERE CONSTRAINT_SCHEMA = DATABASE()
                AND REFERENCED_TABLE_NAME IS NOT NULL
                AND TABLE_NAME <> REFERENCED_TABLE_NAME
            """);
        var selfRefFkColumns = db.Ado.GetDataTable(
            """
            SELECT
                kcu.TABLE_NAME AS table_name,
                kcu.COLUMN_NAME AS column_name
            FROM information_schema.KEY_COLUMN_USAGE AS kcu
            INNER JOIN information_schema.COLUMNS AS c
                ON c.TABLE_SCHEMA = kcu.CONSTRAINT_SCHEMA
                AND c.TABLE_NAME = kcu.TABLE_NAME
                AND c.COLUMN_NAME = kcu.COLUMN_NAME
            WHERE kcu.CONSTRAINT_SCHEMA = DATABASE()
                AND kcu.REFERENCED_TABLE_NAME = kcu.TABLE_NAME
                AND c.IS_NULLABLE = 'YES'
            """);

        if (tables.Rows.Count == 0)
        {
            return;
        }

        var tableNames = tables
            .Rows
            .Cast<DataRow>()
            .Select(row => row["TABLE_NAME"]?.ToString())
            .Where(tableName => !string.IsNullOrWhiteSpace(tableName))
            .Select(tableName => tableName!)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var deleteOrder = GetChildFirstDeleteOrder(tableNames, foreignKeys);

        await db.Ado.ExecuteCommandAsync("SET SESSION FOREIGN_KEY_CHECKS = 0");

        try
        {
            var selfRefColumns = selfRefFkColumns
                .Rows
                .Cast<DataRow>()
                .Where(row => row["table_name"] != DBNull.Value && row["column_name"] != DBNull.Value)
                .GroupBy(row => row["table_name"]!.ToString()!, StringComparer.Ordinal)
                .ToDictionary(
                    table => table.Key,
                    table => table.Select(row => $"`{row["column_name"]!.ToString()}`").Distinct().ToArray(),
                    StringComparer.Ordinal);

            foreach (var entry in selfRefColumns)
            {
                var columns = string.Join(", ", entry.Value.Select(column => $"{column} = NULL"));
                if (columns.Length > 0)
                {
                    await db.Ado.ExecuteCommandAsync($"UPDATE `{entry.Key}` SET {columns}");
                }
            }
        }
        catch
        {
            // Self-referencing foreign key cleanup is best effort.
        }

        try
        {
            foreach (var tableName in deleteOrder)
            {
                var quotedTableName = QuoteIdentifier(tableName);
                await db.Ado.ExecuteCommandAsync($"DROP TABLE IF EXISTS {quotedTableName}");
            }
        }
        finally
        {
            await db.Ado.ExecuteCommandAsync("SET FOREIGN_KEY_CHECKS = 1");
        }
    }

    private static IReadOnlyList<string> GetChildFirstDeleteOrder(IReadOnlyCollection<string> tableNames, DataTable foreignKeys)
    {
        var childrenByParent = foreignKeys
            .Rows
            .Cast<DataRow>()
            .Select(row => new
            {
                Child = row["table_name"]?.ToString(),
                Parent = row["referenced_table_name"]?.ToString()
            })
            .Where(row =>
                !string.IsNullOrWhiteSpace(row.Child) &&
                !string.IsNullOrWhiteSpace(row.Parent) &&
                tableNames.Contains(row.Child) &&
                tableNames.Contains(row.Parent))
            .GroupBy(row => row.Parent!, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Select(row => row.Child!).Distinct(StringComparer.Ordinal).ToArray(),
                StringComparer.Ordinal);

        var visited = new HashSet<string>(StringComparer.Ordinal);
        var visiting = new HashSet<string>(StringComparer.Ordinal);
        var order = new List<string>();

        foreach (var tableName in tableNames.Order(StringComparer.Ordinal))
        {
            Visit(tableName);
        }

        return order;

        void Visit(string tableName)
        {
            if (visited.Contains(tableName) || !visiting.Add(tableName))
            {
                return;
            }

            if (childrenByParent.TryGetValue(tableName, out var children))
            {
                foreach (var child in children.Order(StringComparer.Ordinal))
                {
                    Visit(child);
                }
            }

            visiting.Remove(tableName);
            visited.Add(tableName);
            order.Add(tableName);
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

            if (string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(host, "::1", StringComparison.OrdinalIgnoreCase))
            {
                return "Forbidden test database host: 127.0.0.1/localhost. Integration tests are restricted to development server 192.168.101.199.";
            }

            if (!string.Equals(host, AllowedHost, StringComparison.OrdinalIgnoreCase))
            {
                return $"Forbidden test database host: {host}. Integration tests are restricted to development server {AllowedHost}.";
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
