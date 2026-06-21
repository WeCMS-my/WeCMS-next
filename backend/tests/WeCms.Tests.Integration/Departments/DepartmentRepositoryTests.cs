using SqlSugar;
using WeCms.Data.SqlSugar;
using WeCms.Modules.Organization.Departments;
using WeCms.Modules.Organization.SqlSugar.Repositories;

namespace WeCms.Tests.Integration.Departments;

[Collection(nameof(SharedMySqlCollection))]
public sealed class DepartmentRepositoryTests : PerTestDatabaseResetBase
{
    [DbFact]
    public async Task DepartmentRepository_UsesDepartmentTableForCrudStatusAndAudit()
    {
        var baseConnectionString = IntegrationTestDatabase.GetConnectionString();

        using var db = new SqlSugarClientFactory(baseConnectionString).Create();
        await PrepareDatabaseWithSeedsAsync(db);

        var repository = new DepartmentRepository(db);
        var now = new DateTimeOffset(2026, 6, 21, 0, 0, 0, TimeSpan.Zero);

        var id = await repository.CreateAsync(
            new DepartmentCreateRecord(null, "qa", "Quality", 20, "enabled", now),
            CancellationToken.None);

        var detail = await repository.GetAsync(id, CancellationToken.None);
        var list = await repository.ListAsync(CancellationToken.None);
        var exists = await repository.CodeExistsAsync("qa", null, CancellationToken.None);

        Assert.NotNull(detail);
        Assert.Equal("qa", detail.Code);
        Assert.Equal("Quality", detail.Name);
        Assert.Contains(list, department => department.Id == id);
        Assert.True(exists);
        Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM sys_dept WHERE id = @id", new SugarParameter("@id", id)));

        await repository.SetStatusAsync(id, "disabled", now, CancellationToken.None);
        await repository.RecordAuditAsync(
            new DepartmentAuditRecord(1, "admin", "disable", id, "127.0.0.1", "integration-test", "trace", "success", "Department disabled.", now),
            CancellationToken.None);

        Assert.Equal("disabled", Scalar<string>(db, "SELECT status FROM sys_dept WHERE id = @id", new SugarParameter("@id", id)));
        Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM sys_audit_log WHERE resource = 'department' AND target_id = @targetId", new SugarParameter("@targetId", id.ToString(System.Globalization.CultureInfo.InvariantCulture))));
    }

    [DbFact]
    public async Task DepartmentRepository_DetectsDeleteDependencies()
    {
        var baseConnectionString = IntegrationTestDatabase.GetConnectionString();

        using var db = new SqlSugarClientFactory(baseConnectionString).Create();
        await PrepareDatabaseWithSeedsAsync(db);

        var repository = new DepartmentRepository(db);
        var now = new DateTimeOffset(2026, 6, 21, 0, 0, 0, TimeSpan.Zero);
        var rootId = await repository.CreateAsync(
            new DepartmentCreateRecord(null, "root-test", "Root Test", 10, "enabled", now),
            CancellationToken.None);
        var childId = await repository.CreateAsync(
            new DepartmentCreateRecord(rootId, "child-test", "Child Test", 20, "enabled", now),
            CancellationToken.None);
        db.Ado.ExecuteCommand(
            "UPDATE sys_user SET dept_id = @deptId WHERE username = @username",
            new SugarParameter("@deptId", rootId),
            new SugarParameter("@username", "admin"));

        Assert.True(await repository.HasChildrenAsync(rootId, CancellationToken.None));
        Assert.True(await repository.IsDescendantAsync(rootId, childId, CancellationToken.None));
        Assert.True(await repository.HasUsersAsync(rootId, CancellationToken.None));
    }

    private static T Scalar<T>(ISqlSugarClient db, string sql, params SugarParameter[] parameters)
    {
        var scalar = db.Ado.GetScalar(sql, parameters);

        return scalar is T value
            ? value
            : (T)Convert.ChangeType(scalar, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
    }
}
