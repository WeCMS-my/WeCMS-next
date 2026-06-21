namespace WeCms.Tests.Architecture;

public sealed class AccessControlMigrationSpecTests
{
    private static readonly string SpecRoot = Path.Combine(TestPaths.RepoRoot, "docs", "specs", "s5-accesscontrol-migration");

    [Fact]
    public async Task S5AccessControlMigrationSpecTrio_ExistsAndDefinesScope()
    {
        var specPath = Path.Combine(SpecRoot, "spec.md");
        var tasksPath = Path.Combine(SpecRoot, "tasks.md");
        var checklistPath = Path.Combine(SpecRoot, "checklist.md");

        Assert.True(File.Exists(specPath), "Missing S5 AccessControl migration spec.md.");
        Assert.True(File.Exists(tasksPath), "Missing S5 AccessControl migration tasks.md.");
        Assert.True(File.Exists(checklistPath), "Missing S5 AccessControl migration checklist.md.");

        var spec = await File.ReadAllTextAsync(specPath, TestContext.Current.CancellationToken);
        var tasks = await File.ReadAllTextAsync(tasksPath, TestContext.Current.CancellationToken);
        var checklist = await File.ReadAllTextAsync(checklistPath, TestContext.Current.CancellationToken);

        foreach (var token in new[] { "Roles", "Permissions", "Menus", "PermissionDefinition", "URL permission", "button permission", "AccessProfile" })
        {
            Assert.Contains(token, spec, StringComparison.Ordinal);
            Assert.Contains(token, tasks, StringComparison.Ordinal);
            Assert.Contains(token, checklist, StringComparison.Ordinal);
        }

        Assert.Contains("WeCms.Modules.AccessControl", spec, StringComparison.Ordinal);
        Assert.Contains("WeCms.Modules.AccessControl.SqlSugar", spec, StringComparison.Ordinal);
        Assert.Contains("Do not migrate Organization", spec, StringComparison.Ordinal);
        Assert.Contains("Do not change existing permission code strings", spec, StringComparison.Ordinal);
    }
}
