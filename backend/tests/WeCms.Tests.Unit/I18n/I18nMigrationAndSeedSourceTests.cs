namespace WeCms.Tests.Unit.I18n;

public sealed class I18nMigrationAndSeedSourceTests
{
    [Fact]
    public async Task H2I18nMigration_CreatesMessageTableWithUniqueLocaleMessageKey()
    {
        var source = await File.ReadAllTextAsync(Path.Combine(RepoRoot(), "database", "migrations", "000001_baseline_system_schema.sql"), TestContext.Current.CancellationToken);

        Assert.Contains("CREATE TABLE sys_i18n_message", source, StringComparison.Ordinal);
        Assert.Contains("locale", source, StringComparison.Ordinal);
        Assert.Contains("message_key", source, StringComparison.Ordinal);
        Assert.Contains("message_value", source, StringComparison.Ordinal);
        Assert.Contains("UNIQUE KEY uq_sys_i18n_message_locale_key", source, StringComparison.Ordinal);
        Assert.Contains("locale, message_key", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task H2I18nPermissionSeed_ContainsManagementAndAccountPermissions()
    {
        var source = await File.ReadAllTextAsync(Path.Combine(RepoRoot(), "database", "seeds", "000002_seed_system_permissions.sql"), TestContext.Current.CancellationToken);

        foreach (var permission in new[]
        {
            "sys:i18n:page",
            "sys:i18n:list",
            "sys:i18n:detail",
            "sys:i18n:create",
            "sys:i18n:update",
            "sys:i18n:delete",
            "account:i18n:switch"
        })
        {
            Assert.Contains(permission, source, StringComparison.Ordinal);
        }
    }

    private static string RepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "AGENTS.md")))
        {
            current = current.Parent;
        }

        return current?.FullName ?? throw new InvalidOperationException("Repository root was not found.");
    }
}
