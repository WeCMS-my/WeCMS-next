namespace WeCms.Tests.Unit.Security;

public sealed class SecurityBanMigrationSourceTests
{
    [Fact]
    public async Task H1SecurityBanMigration_CreatesSecurityBanTable()
    {
        var source = await File.ReadAllTextAsync(RepoPath("database", "migrations", "000013_h1_security_ban.sql"), TestContext.Current.CancellationToken);

        Assert.Contains("CREATE TABLE sys_security_ban", source, StringComparison.Ordinal);
        Assert.Contains("ban_type VARCHAR(32) NOT NULL", source, StringComparison.Ordinal);
        Assert.Contains("target VARCHAR(128) NOT NULL", source, StringComparison.Ordinal);
        Assert.Contains("expires_at DATETIME(6) NULL", source, StringComparison.Ordinal);
        Assert.Contains("revoked_at DATETIME(6) NULL", source, StringComparison.Ordinal);
        Assert.Contains("revoked_by BIGINT NULL", source, StringComparison.Ordinal);
        Assert.Contains("revoke_reason VARCHAR(500) NULL", source, StringComparison.Ordinal);
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
