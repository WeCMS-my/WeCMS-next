using SqlSugar;
using WeCms.Data.SqlSugar;
using WeCms.Modules.Audit.Logs;
using WeCms.Modules.Audit.SqlSugar.Repositories;

namespace WeCms.Tests.Integration.Audit;

[Collection(nameof(SharedMySqlCollection))]
public sealed class LogRepositoryTests : PerTestDatabaseResetBase
{
    [DbFact]
    public async Task LogRepository_UsesLoginAndAuditTablesForQueries()
    {
        var baseConnectionString = IntegrationTestDatabase.GetConnectionString();

        using var db = new SqlSugarClientFactory(baseConnectionString).Create();
        await PrepareDatabaseWithSeedsAsync(db);

        var now = new DateTimeOffset(2026, 6, 21, 0, 0, 0, TimeSpan.Zero);
        var username = $"audit-{Guid.NewGuid():N}"[..24];
        var ip = "192.168.101.199";
        db.Ado.ExecuteCommand(
            """
            INSERT INTO sys_login_log (username, user_id, ip, user_agent, result, reason, created_at)
            VALUES (@username, @userId, @ip, @userAgent, @result, @reason, @createdAt)
            """,
            new SugarParameter("@username", username),
            new SugarParameter("@userId", 7),
            new SugarParameter("@ip", ip),
            new SugarParameter("@userAgent", "integration-test"),
            new SugarParameter("@result", "success"),
            new SugarParameter("@reason", "ok"),
            new SugarParameter("@createdAt", now.UtcDateTime));
        db.Ado.ExecuteCommand(
            """
            INSERT INTO sys_audit_log (user_id, username, module, resource, action, target_id, request_method, request_path, ip_address, user_agent, trace_id, result, detail, created_at)
            VALUES (@userId, @username, @module, @resource, @action, @targetId, @requestMethod, @requestPath, @ipAddress, @userAgent, @traceId, @result, @detail, @createdAt)
            """,
            new SugarParameter("@userId", 7),
            new SugarParameter("@username", username),
            new SugarParameter("@module", "system"),
            new SugarParameter("@resource", "audit-test"),
            new SugarParameter("@action", "query"),
            new SugarParameter("@targetId", "target-1"),
            new SugarParameter("@requestMethod", "GET"),
            new SugarParameter("@requestPath", "/api/v1/system/audit-logs"),
            new SugarParameter("@ipAddress", ip),
            new SugarParameter("@userAgent", "integration-test"),
            new SugarParameter("@traceId", "trace-audit-repository"),
            new SugarParameter("@result", "success"),
            new SugarParameter("@detail", "Audit repository integration test."),
            new SugarParameter("@createdAt", now.UtcDateTime));

        var repository = new LogRepository(db);

        var loginList = await repository.ListLoginLogsAsync(
            new LoginLogListCriteria(1, 20, username, ip, "success", now.AddMinutes(-1), now.AddMinutes(1)),
            CancellationToken.None);
        var loginDetail = await repository.GetLoginLogAsync(loginList.Records.Single().Id, CancellationToken.None);
        var auditList = await repository.ListAuditLogsAsync(
            new AuditLogListCriteria(1, 20, username, "system", "audit-test", "query", "success", now.AddMinutes(-1), now.AddMinutes(1)),
            CancellationToken.None);
        var auditDetail = await repository.GetAuditLogAsync(auditList.Records.Single().Id, CancellationToken.None);

        Assert.Equal(1, loginList.Total);
        Assert.NotNull(loginDetail);
        Assert.Equal(username, loginDetail.Username);
        Assert.Equal("integration-test", loginDetail.UserAgent);
        Assert.Equal(1, auditList.Total);
        Assert.NotNull(auditDetail);
        Assert.Equal("trace-audit-repository", auditDetail.TraceId);
        Assert.Equal("/api/v1/system/audit-logs", auditDetail.RequestPath);
        Assert.Null(await repository.GetLoginLogAsync(-1, CancellationToken.None));
        Assert.Null(await repository.GetAuditLogAsync(-1, CancellationToken.None));
    }
}
