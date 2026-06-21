using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using SqlSugar;
using WeCms.Api.Extensions;
using WeCms.Data.SqlSugar;

namespace WeCms.Tests.Unit.Diagnostics;

public sealed class MiniProfilerDiagnosticsTests
{
    [Fact]
    public void MiniProfiler_RegisteredInDevelopment()
    {
        var environment = new TestHostEnvironment(Environments.Development);

        Assert.True(WeCmsDiagnosticsExtensions.IsMiniProfilerEnabled(environment, EmptyConfiguration()));
    }

    [Fact]
    public void MiniProfiler_NotEnabledByDefaultInNonDevelopment()
    {
        var environment = new TestHostEnvironment(Environments.Production);

        Assert.False(WeCmsDiagnosticsExtensions.IsMiniProfilerEnabled(environment, EmptyConfiguration()));
    }

    [Fact]
    public void MiniProfiler_CanBeEnabledOutsideDevelopmentWithExplicitConfiguration()
    {
        var environment = new TestHostEnvironment(Environments.Production);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Diagnostics:MiniProfiler:Enabled"] = "true"
            })
            .Build();

        Assert.True(WeCmsDiagnosticsExtensions.IsMiniProfilerEnabled(environment, configuration));
    }

    [Fact]
    public async Task MiniProfiler_RecordsHttpTiming()
    {
        var source = await File.ReadAllTextAsync(
            RepoPath("backend", "src", "WeCms.Api", "Extensions", "WeCmsDiagnosticsExtensions.cs"),
            TestContext.Current.CancellationToken);

        Assert.Contains("services.AddMiniProfiler", source, StringComparison.Ordinal);
        Assert.Contains("app.UseMiniProfiler()", source, StringComparison.Ordinal);
        Assert.DoesNotContain("AddControllers", source, StringComparison.Ordinal);
        Assert.DoesNotContain("MapControllers", source, StringComparison.Ordinal);
    }

    [Fact]
    public void SqlTiming_DoesNotExposeSensitiveParameters()
    {
        var redacted = new SqlAuditRedactor().Redact(
        [
            new SugarParameter("@username", "admin"),
            new SugarParameter("@password", "plain-secret")
        ]);
        var command = MiniProfilerSqlTimingFormatter.Command(new SqlTimingRecord(
            "main",
            "INSERT",
            "INSERT INTO sys_user (username, password_hash) VALUES (@username, @password)",
            redacted,
            TimeSpan.FromMilliseconds(3)));

        Assert.Contains("@username=admin", command, StringComparison.Ordinal);
        Assert.Contains("@password=***REDACTED***", command, StringComparison.Ordinal);
        Assert.DoesNotContain("plain-secret", command, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SqlTiming_UsesSqlExecutionElapsedTime()
    {
        var source = await File.ReadAllTextAsync(
            RepoPath("backend", "src", "WeCms.Api", "Extensions", "WeCmsDiagnosticsExtensions.cs"),
            TestContext.Current.CancellationToken);

        Assert.Contains("DurationMilliseconds = elapsedMs", source, StringComparison.Ordinal);
        Assert.Contains("profiler.Head.AddCustomTiming(\"sql\", timing)", source, StringComparison.Ordinal);
    }

    private static IConfiguration EmptyConfiguration()
    {
        return new ConfigurationBuilder().Build();
    }

    private static string RepoPath(params string[] segments)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "backend", "WeCms.slnx")))
            {
                return Path.Combine([directory.FullName, .. segments]);
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "WeCms.Tests";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
