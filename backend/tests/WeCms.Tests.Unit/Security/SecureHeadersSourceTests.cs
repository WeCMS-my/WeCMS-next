namespace WeCms.Tests.Unit.Security;

public sealed class SecureHeadersSourceTests
{
    [Fact]
    public async Task Program_RegistersSecureHeadersMiddleware()
    {
        var program = await File.ReadAllTextAsync(RepoPath("backend", "src", "WeCms.Api", "Program.cs"), TestContext.Current.CancellationToken);

        Assert.Contains("UseMiddleware<SecureHeadersMiddleware>", program, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SecureHeadersMiddleware_EmitsBaseHeadersAndConfigurableCsp()
    {
        var middlewarePath = RepoPath("backend", "src", "WeCms.Api", "Middleware", "SecureHeadersMiddleware.cs");
        Assert.True(File.Exists(middlewarePath), "SecureHeadersMiddleware.cs must exist.");

        var source = await File.ReadAllTextAsync(middlewarePath, TestContext.Current.CancellationToken);

        Assert.Contains("X-Content-Type-Options", source, StringComparison.Ordinal);
        Assert.Contains("X-Frame-Options", source, StringComparison.Ordinal);
        Assert.Contains("Referrer-Policy", source, StringComparison.Ordinal);
        Assert.Contains("Permissions-Policy", source, StringComparison.Ordinal);
        Assert.Contains("Content-Security-Policy-Report-Only", source, StringComparison.Ordinal);
        Assert.Contains("Content-Security-Policy", source, StringComparison.Ordinal);
        Assert.Contains("CspEnabled", source, StringComparison.Ordinal);
        Assert.Contains("CspReportOnlyEnabled", source, StringComparison.Ordinal);
        Assert.Contains("Vite", source, StringComparison.Ordinal);
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
}
