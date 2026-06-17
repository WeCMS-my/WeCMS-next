using System.Text.Json;
using SqlSugar;
using WeCms.Persistence.Data;
using Xunit;

namespace WeCms.Tests.Integration;

internal static class IntegrationTestDatabase
{
    private const string EnvVarName = "WECMS_TEST_MYSQL_CONNECTION_STRING";

    public static string GetConnectionString()
    {
        var connectionString = Environment.GetEnvironmentVariable(EnvVarName)
            ?? ReadConnectionStringFromAppSettings("Test")
            ?? ReadConnectionStringFromAppSettings("Default");

        Assert.False(
            string.IsNullOrWhiteSpace(connectionString),
            "Set WECMS_TEST_MYSQL_CONNECTION_STRING or configure backend/src/WeCms.Api/appsettings.Development.json (ConnectionStrings:Test/Default) to run MySQL integration tests.");

        return connectionString!;
    }

    public static bool IsDatabaseAvailable([System.Diagnostics.CodeAnalysis.NotNullWhen(false)] out string? unavailableReason)
    {
        unavailableReason = null;
        try
        {
            using var db = new SqlSugarClientFactory(GetConnectionString()).Create();
            db.Ado.GetScalar("SELECT 1");
            return true;
        }
        catch (Exception ex)
        {
            unavailableReason = ex.Message;
            return false;
        }
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
