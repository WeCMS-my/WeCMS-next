using SqlSugar;
using WeCms.Data.SqlSugar;
using WeCms.Modules.AccessControl.SqlSugar.Repositories;

namespace WeCms.Tests.Integration.Menus;

[Collection(nameof(SharedMySqlCollection))]
public sealed class MenuRepositoryTests : PerTestDatabaseResetBase
{
    [DbFact]
    public async Task ListAsync_ReturnsSeededSystemMenuAndPermissionBindings()
    {
        var baseConnectionString = IntegrationTestDatabase.GetConnectionString();

        using var db = new SqlSugarClientFactory(baseConnectionString).Create();
        await PrepareDatabaseWithSeedsAsync(db);

        var repository = new MenuRepository(db);

        var menus = await repository.ListAsync(CancellationToken.None);

        var root = Assert.Single(menus, menu => menu.Code == "sys.system");
        Assert.Null(root.ParentId);
        Assert.Equal("catalog", root.Type);
        Assert.True(root.IsBuiltin);

        var menuManagement = Assert.Single(menus, menu => menu.Code == "sys.menus");
        Assert.Equal(root.Id, menuManagement.ParentId);
        Assert.Equal("sys:menu:page", menuManagement.PermissionCode);
        Assert.Equal("enabled", menuManagement.Status);
    }
}
