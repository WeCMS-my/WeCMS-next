using SqlSugar;
using WeCms.Modules.System.Roles;
using WeCms.Persistence.Data;
using WeCms.Persistence.Migration;
using WeCms.Persistence.Modules.System.Roles;
using WeCms.Tests.Integration;

namespace WeCms.Tests.Integration.Roles;

[Collection(nameof(SharedMySqlCollection))]
public sealed class RoleRepositoryTests : PerTestDatabaseResetBase
{
    [DbFact]
    public async Task ExistingPermissionIdsAsync_FiltersDeletedPermissions()
    {
        var baseConnectionString = RequiredConnectionString();

        try
        {
            using var db = new SqlSugarClientFactory(baseConnectionString).Create();
            await PrepareDatabaseWithSeedsAsync(db);

            var permissionId = Scalar<long>(
                db,
                "SELECT id FROM sys_permission ORDER BY id LIMIT 1");
            Assert.True(permissionId > 0);
            db.Ado.ExecuteCommand(
                "UPDATE sys_permission SET deleted_at = @deletedAt WHERE id = @id",
                new SugarParameter("@deletedAt", DateTime.UtcNow),
                new SugarParameter("@id", permissionId));

            var repository = new RoleRepository(db);
            var existing = await repository.ExistingPermissionIdsAsync([permissionId], CancellationToken.None);

            Assert.Empty(existing);
        }
        finally
        {
        }
    }

    [DbFact]
    public async Task ExistingMenuIdsAsync_FiltersDeletedMenus()
    {
        var baseConnectionString = RequiredConnectionString();

        try
        {
            using var db = new SqlSugarClientFactory(baseConnectionString).Create();
            await PrepareDatabaseWithSeedsAsync(db);

            var menuId = Scalar<long>(
                db,
                "SELECT id FROM sys_menu ORDER BY id LIMIT 1");
            Assert.True(menuId > 0);
            db.Ado.ExecuteCommand(
                "UPDATE sys_menu SET deleted_at = @deletedAt WHERE id = @id",
                new SugarParameter("@deletedAt", DateTime.UtcNow),
                new SugarParameter("@id", menuId));

            var repository = new RoleRepository(db);
            var existing = await repository.ExistingMenuIdsAsync([menuId], CancellationToken.None);

            Assert.Empty(existing);
        }
        finally
        {
        }
    }

    private static T Scalar<T>(ISqlSugarClient db, string sql, params SugarParameter[] parameters)
    {
        var scalar = db.Ado.GetScalar(sql, parameters);

        if (scalar is T value)
        {
            return value;
        }

        return (T)Convert.ChangeType(scalar, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string RequiredConnectionString()
    {
        return IntegrationTestDatabase.GetConnectionString();
    }
}
