using SqlSugar;
using WeCms.Data.SqlSugar;
using WeCms.Modules.Organization.Positions;
using WeCms.Modules.Organization.SqlSugar.Repositories;

namespace WeCms.Tests.Integration.Positions;

[Collection(nameof(SharedMySqlCollection))]
public sealed class PositionRepositoryTests : PerTestDatabaseResetBase
{
    [DbFact]
    public async Task PositionRepository_UsesPositionTablesForCrud()
    {
        var baseConnectionString = IntegrationTestDatabase.GetConnectionString();

        using var db = new SqlSugarClientFactory(baseConnectionString).Create();
        await PrepareDatabaseWithSeedsAsync(db);

        var repository = new PositionRepository(db);
        var now = new DateTimeOffset(2026, 6, 21, 0, 0, 0, TimeSpan.Zero);

        var id = await repository.CreateAsync(
            new PositionCreateRecord("qa", "Quality Analyst", 20, "enabled", now),
            CancellationToken.None);

        var detail = await repository.GetAsync(id, CancellationToken.None);
        var list = await repository.ListAsync(new PositionListCriteria(1, 20, "Quality", "enabled"), CancellationToken.None);
        var exists = await repository.CodeExistsAsync("qa", null, CancellationToken.None);

        Assert.NotNull(detail);
        Assert.Equal("qa", detail.Code);
        Assert.Equal("Quality Analyst", detail.Name);
        Assert.Contains(list.Records, position => position.Id == id);
        Assert.True(exists);
        Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM sys_position WHERE id = @id", new SugarParameter("@id", id)));

        await repository.SetStatusAsync(id, "disabled", now, CancellationToken.None);

        Assert.Equal("disabled", Scalar<string>(db, "SELECT status FROM sys_position WHERE id = @id", new SugarParameter("@id", id)));
    }

    [DbFact]
    public async Task ExistingIdsAsync_FiltersDeletedPositions()
    {
        var baseConnectionString = IntegrationTestDatabase.GetConnectionString();

        using var db = new SqlSugarClientFactory(baseConnectionString).Create();
        await PrepareDatabaseWithSeedsAsync(db);

        var firstPositionIdObj = db.Ado.GetScalar("SELECT id FROM sys_position ORDER BY id LIMIT 1");
        var firstPositionId = firstPositionIdObj is not null && firstPositionIdObj is not DBNull
            ? Convert.ToInt64(firstPositionIdObj, System.Globalization.CultureInfo.InvariantCulture)
            : 0;
        var positionId = firstPositionId > 0
            ? firstPositionId
            : InsertPositionAndGetId(db, "test_position_exist_filters");
        Assert.True(positionId > 0);

        db.Ado.ExecuteCommand(
            "UPDATE sys_position SET deleted_at = @deletedAt WHERE id = @id",
            new SugarParameter("@deletedAt", DateTime.UtcNow),
            new SugarParameter("@id", positionId));

        var repository = new PositionRepository(db);
        var existing = await repository.ExistingIdsAsync([positionId], CancellationToken.None);

        Assert.Empty(existing);
    }

    private static long InsertPositionAndGetId(ISqlSugarClient db, string code)
    {
        var now = DateTime.UtcNow;
        var uniqueCode = $"{code}_{Guid.NewGuid():N}";
        db.Ado.ExecuteCommand(
            """
            INSERT INTO sys_position(code, name, sort_order, status, created_at, updated_at)
            VALUES (@code, @name, 0, 'enabled', @now, @now)
            """,
            new SugarParameter("@code", uniqueCode),
            new SugarParameter("@name", uniqueCode),
            new SugarParameter("@now", now));

        var positionId = db.Ado.GetScalar(
            "SELECT id FROM sys_position WHERE code = @code LIMIT 1",
            new SugarParameter("@code", uniqueCode));
        if (positionId is null or DBNull)
        {
            throw new InvalidOperationException("InsertPositionAndGetId: failed to read newly inserted position id.");
        }

        return Convert.ToInt64(positionId, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static T Scalar<T>(ISqlSugarClient db, string sql, params SugarParameter[] parameters)
    {
        var scalar = db.Ado.GetScalar(sql, parameters);

        return scalar is T value
            ? value
            : (T)Convert.ChangeType(scalar, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
    }
}
