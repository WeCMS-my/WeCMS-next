using SqlSugar;
using WeCms.Data.SqlSugar;
using WeCms.Modules.AccessControl.SqlSugar.Repositories;

namespace WeCms.Tests.Integration.AccessProfiles;

[Collection(nameof(SharedMySqlCollection))]
public sealed class AccessProfileRepositoryTests : PerTestDatabaseResetBase
{
    [DbFact]
    public async Task GetAsyncReadsSeededAdminRolesPermissionsAndMenus()
    {
        var baseConnectionString = IntegrationTestDatabase.GetConnectionString();

        using var db = new SqlSugarClientFactory(baseConnectionString).Create();
        await PrepareDatabaseWithSeedsAsync(db);

        var repository = new AccessProfileRepository(db);
        var adminUserId = Scalar<long>(db, "SELECT id FROM sys_user WHERE username = 'admin' LIMIT 1");

        var version = await repository.GetPermissionVersionAsync(adminUserId, CancellationToken.None);
        var roles = await repository.ListRoleCodesAsync(adminUserId, CancellationToken.None);
        var permissions = await repository.ListPermissionCodesAsync(adminUserId, CancellationToken.None);
        var menus = await repository.ListVisibleMenusAsync(adminUserId, CancellationToken.None);

        Assert.True(version >= 0);
        Assert.Contains("super_admin", roles);
        Assert.Contains("sys:menu:page", permissions);

        var menuManagement = Assert.Single(menus, menu => menu.Code == "sys.menus");
        Assert.Equal("sys:menu:page", menuManagement.PermissionCode);
        Assert.Contains(menus, menu => menu.Id == menuManagement.ParentId && menu.Code == "sys.system");
    }

    private static T Scalar<T>(ISqlSugarClient db, string sql, params SugarParameter[] parameters)
    {
        var scalar = db.Ado.GetScalar(sql, parameters);

        return scalar is T value
            ? value
            : (T)Convert.ChangeType(scalar, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
    }
}
