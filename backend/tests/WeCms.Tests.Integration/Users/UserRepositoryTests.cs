using SqlSugar;
using WeCms.Persistence.Data;
using WeCms.Persistence.Modules.System.Users;
using WeCms.Shared.Security;

namespace WeCms.Tests.Integration.Users;

[Collection(nameof(SharedMySqlCollection))]
public sealed class UserRepositoryTests : PerTestDatabaseResetBase
{
    [DbFact]
    public async Task ReplaceRolesAsync_ThrowsWhenTargetUserDoesNotExist()
    {
        var connectionString = RequiredConnectionString();
        using var db = new SqlSugarClientFactory(connectionString).Create();
        await PrepareDatabaseWithSeedsAsync(db);

        var roleId = Scalar<long>(db, "SELECT id FROM sys_role WHERE code = 'super_admin'");
        var repository = new UserRepository(db, new SecurityEventClassifier());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repository.ReplaceRolesAsync(999999, [], DateTimeOffset.UtcNow, CancellationToken.None));

        Assert.Equal("Expected one affected row, got 0.", exception.Message);
    }

    [DbFact]
    public async Task ExistingRoleIdsAsync_FiltersDeletedRoles()
    {
        var connectionString = RequiredConnectionString();
        using var db = new SqlSugarClientFactory(connectionString).Create();
        await PrepareDatabaseWithSeedsAsync(db);

        var roleId = Scalar<long>(db, "SELECT id FROM sys_role ORDER BY id LIMIT 1");
        Assert.True(roleId > 0);

        db.Ado.ExecuteCommand(
            "UPDATE sys_role SET deleted_at = @deletedAt WHERE id = @id",
            new SugarParameter("@deletedAt", DateTime.UtcNow),
            new SugarParameter("@id", roleId));

        var repository = new UserRepository(db, new SecurityEventClassifier());
        var existing = await repository.ExistingRoleIdsAsync([roleId], CancellationToken.None);

        Assert.Empty(existing);
    }

    [DbFact]
    public async Task ExistingPostIdsAsync_FiltersDeletedPosts()
    {
        var connectionString = RequiredConnectionString();
        using var db = new SqlSugarClientFactory(connectionString).Create();
        await PrepareDatabaseWithSeedsAsync(db);

        var firstPostIdObj = db.Ado.GetScalar("SELECT id FROM sys_post ORDER BY id LIMIT 1");
        var firstPostId = firstPostIdObj is not null && firstPostIdObj is not DBNull
            ? Convert.ToInt64(firstPostIdObj, System.Globalization.CultureInfo.InvariantCulture)
            : 0;
        var postId = firstPostId > 0
            ? firstPostId
            : InsertPostAndGetId(db, "test_post_exist_filters");
        Assert.True(postId > 0);

        db.Ado.ExecuteCommand(
            "UPDATE sys_post SET deleted_at = @deletedAt WHERE id = @id",
            new SugarParameter("@deletedAt", DateTime.UtcNow),
            new SugarParameter("@id", postId));

        var repository = new UserRepository(db, new SecurityEventClassifier());
        var existing = await repository.ExistingPostIdsAsync([postId], CancellationToken.None);

        Assert.Empty(existing);
    }

    private static long InsertPostAndGetId(ISqlSugarClient db, string code)
    {
        var now = DateTime.UtcNow;
        var uniqueCode = $"{code}_{Guid.NewGuid():N}";
        db.Ado.ExecuteCommand(
            """
            INSERT INTO sys_post(code, name, sort_order, status, created_at, updated_at)
            VALUES (@code, @name, 0, 'enabled', @now, @now)
            """,
            new SugarParameter("@code", uniqueCode),
            new SugarParameter("@name", uniqueCode),
            new SugarParameter("@now", now));

        var postId = db.Ado.GetScalar("SELECT id FROM sys_post WHERE code = @code LIMIT 1",
            new SugarParameter("@code", uniqueCode));
        if (postId is null or DBNull)
        {
            throw new InvalidOperationException("InsertPostAndGetId: failed to read newly inserted post id.");
        }

        return Convert.ToInt64(postId, System.Globalization.CultureInfo.InvariantCulture);
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
