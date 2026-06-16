namespace WeCms.Tests.Architecture;

public sealed class M1SystemSeedScanTests
{
    private static readonly string[] RequiredPermissionCodes =
    [
        "sys:user:page",
        "sys:user:list",
        "sys:user:detail",
        "sys:user:create",
        "sys:user:update",
        "sys:user:delete",
        "sys:user:enable",
        "sys:user:disable",
        "sys:user:reset-password",
        "sys:user:assign-role",
        "sys:user:assign-post",
        "sys:role:page",
        "sys:role:list",
        "sys:role:detail",
        "sys:role:create",
        "sys:role:update",
        "sys:role:delete",
        "sys:role:enable",
        "sys:role:disable",
        "sys:role:assign-permission",
        "sys:role:assign-menu",
        "sys:menu:page",
        "sys:menu:list",
        "sys:menu:tree",
        "sys:menu:detail",
        "sys:menu:create",
        "sys:menu:update",
        "sys:menu:delete",
        "sys:menu:enable",
        "sys:menu:disable",
        "sys:permission:page",
        "sys:permission:list",
        "sys:permission:tree",
        "sys:permission:detail",
        "sys:permission:create",
        "sys:permission:update",
        "sys:permission:delete",
        "sys:permission:enable",
        "sys:permission:disable",
        "sys:dept:page",
        "sys:dept:list",
        "sys:dept:tree",
        "sys:dept:detail",
        "sys:dept:create",
        "sys:dept:update",
        "sys:dept:delete",
        "sys:dept:enable",
        "sys:dept:disable",
        "sys:post:page",
        "sys:post:list",
        "sys:post:detail",
        "sys:post:create",
        "sys:post:update",
        "sys:post:delete",
        "sys:post:enable",
        "sys:post:disable",
        "sys:dict:page",
        "sys:dict:type:list",
        "sys:dict:type:create",
        "sys:dict:type:update",
        "sys:dict:type:delete",
        "sys:dict:value:list",
        "sys:dict:value:create",
        "sys:dict:value:update",
        "sys:dict:value:delete",
        "sys:setting:page",
        "sys:setting:list",
        "sys:setting:detail",
        "sys:setting:update",
        "sys:login-log:page",
        "sys:login-log:list",
        "sys:login-log:detail",
        "sys:audit-log:page",
        "sys:audit-log:list",
        "sys:audit-log:detail",
        "sys:security-event:page",
        "sys:security-event:list",
        "sys:security-event:detail",
        "sys:file:page",
        "sys:file:list",
        "sys:file:detail",
        "sys:file:upload",
        "sys:file:delete"
    ];

    [Fact]
    public async Task M1SystemPermissionSeed_ContainsEveryPlannedPermissionCode()
    {
        var source = await ReadSeedAsync("000003_seed_m1_system_permissions.sql");

        foreach (var code in RequiredPermissionCodes)
        {
            Assert.Contains(code, source, StringComparison.Ordinal);
        }

        Assert.Equal(RequiredPermissionCodes.Length, RequiredPermissionCodes.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public async Task M1SystemMenuSeed_UsesCurrentMenuNameAsStableCode()
    {
        var source = await ReadSeedAsync("000004_seed_m1_system_menus.sql");

        Assert.Contains("'sys.users'", source, StringComparison.Ordinal);
        Assert.Contains("'sys.roles'", source, StringComparison.Ordinal);
        Assert.Contains("'sys.files'", source, StringComparison.Ordinal);
        Assert.Contains("WHERE NOT EXISTS", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task M1RolePermissionSeed_GrantsSuperAdminEveryPermission()
    {
        var source = await ReadSeedAsync("000005_seed_m1_role_permissions.sql");

        Assert.Contains("JOIN sys_permission p", source, StringComparison.Ordinal);
        Assert.Contains("WHERE r.code = 'super_admin'", source, StringComparison.Ordinal);
        Assert.Contains("WHERE rp.role_id = r.id", source, StringComparison.Ordinal);
        Assert.DoesNotContain("p.code IN", source, StringComparison.Ordinal);
    }

    private static Task<string> ReadSeedAsync(string fileName)
    {
        return File.ReadAllTextAsync(Path.Combine(TestPaths.RepoRoot, "database", "seeds", fileName));
    }
}
