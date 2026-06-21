using SqlSugar;
using WeCms.Data.SqlSugar;
using WeCms.Modules.Configuration.Dicts;
using WeCms.Modules.Configuration.SqlSugar.Repositories;

namespace WeCms.Tests.Integration.Dicts;

[Collection(nameof(SharedMySqlCollection))]
public sealed class DictRepositoryTests : PerTestDatabaseResetBase
{
    [DbFact]
    public async Task DictRepository_UsesDictTablesForTypeAndValueCrud()
    {
        var baseConnectionString = IntegrationTestDatabase.GetConnectionString();

        using var db = new SqlSugarClientFactory(baseConnectionString).Create();
        await PrepareDatabaseWithSeedsAsync(db);

        var repository = new DictRepository(db);
        var now = new DateTimeOffset(2026, 6, 21, 0, 0, 0, TimeSpan.Zero);
        var typeCode = $"integration_{Guid.NewGuid():N}"[..44];

        var typeId = await repository.CreateTypeAsync(
            new DictTypeCreateRecord(typeCode, "Integration Status", "integration test type", 10, "enabled", now),
            CancellationToken.None);

        var typeDetail = await repository.GetTypeAsync(typeId, CancellationToken.None);
        var typeByCode = await repository.GetTypeByCodeAsync(typeCode, CancellationToken.None);
        var typeList = await repository.ListTypesAsync(new DictTypeListCriteria(1, 20, "Integration", "enabled"), CancellationToken.None);

        Assert.NotNull(typeDetail);
        Assert.NotNull(typeByCode);
        Assert.Equal(typeCode, typeDetail.Code);
        Assert.Equal(typeId, typeByCode.Id);
        Assert.Contains(typeList.Records, type => type.Id == typeId);
        Assert.True(await repository.TypeCodeExistsAsync(typeCode, null, CancellationToken.None));
        Assert.False(await repository.TypeCodeExistsAsync(typeCode, typeId, CancellationToken.None));

        var valueId = await repository.CreateValueAsync(
            new DictValueCreateRecord(typeId, "Draft", "draft", "integration test value", 1, true, "enabled", now),
            CancellationToken.None);

        var values = await repository.ListValuesAsync(typeCode, CancellationToken.None);
        var valueDetail = await repository.GetValueAsync(valueId, CancellationToken.None);

        Assert.NotNull(valueDetail);
        Assert.Contains(values, value => value.Id == valueId);
        Assert.True(await repository.ValueExistsAsync(typeId, "draft", null, CancellationToken.None));
        Assert.False(await repository.ValueExistsAsync(typeId, "draft", valueId, CancellationToken.None));
        Assert.True(await repository.TypeHasValuesAsync(typeId, CancellationToken.None));

        await repository.UpdateTypeAsync(
            new DictTypeUpdateRecord(typeId, "Integration Status Updated", null, 20, "enabled", now),
            CancellationToken.None);
        await repository.UpdateValueAsync(
            new DictValueUpdateRecord(valueId, "Published", "published", null, 2, false, "enabled", now),
            CancellationToken.None);
        await repository.SetTypeStatusAsync(typeId, "disabled", now, CancellationToken.None);
        await repository.DisableValuesByTypeAsync(typeId, now, CancellationToken.None);
        await repository.SetValueStatusAsync(valueId, "enabled", now, CancellationToken.None);

        Assert.Equal("disabled", Scalar<string>(db, "SELECT status FROM sys_dict_type WHERE id = @id", new SugarParameter("@id", typeId)));
        Assert.Equal("enabled", Scalar<string>(db, "SELECT status FROM sys_dict_value WHERE id = @id", new SugarParameter("@id", valueId)));
        Assert.Equal("published", Scalar<string>(db, "SELECT value FROM sys_dict_value WHERE id = @id", new SugarParameter("@id", valueId)));

        await repository.RecordAuditAsync(
            new DictAuditRecord(1, "admin", "update-type", typeId, "dict-type", "127.0.0.1", "integration-test", "trace-dict", "success", "Dictionary type updated.", now),
            CancellationToken.None);
        Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM sys_audit_log WHERE trace_id = 'trace-dict' AND resource = 'dict-type'"));

        await repository.SoftDeleteValueAsync(valueId, now, CancellationToken.None);
        await repository.SoftDeleteTypeAsync(typeId, now, CancellationToken.None);

        Assert.Null(await repository.GetValueAsync(valueId, CancellationToken.None));
        Assert.Null(await repository.GetTypeAsync(typeId, CancellationToken.None));
    }

    private static T Scalar<T>(ISqlSugarClient db, string sql, params SugarParameter[] parameters)
    {
        var scalar = db.Ado.GetScalar(sql, parameters);

        return scalar is T value
            ? value
            : (T)Convert.ChangeType(scalar, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
    }
}
