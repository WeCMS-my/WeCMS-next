using SqlSugar;
using WeCms.Modules.System.I18n;
using WeCms.Persistence.Data;
using WeCms.Persistence.Modules.System.I18n;

namespace WeCms.Tests.Integration.I18n;

[Collection(nameof(SharedMySqlCollection))]
public sealed class I18nMessageIntegrationTests : PerTestDatabaseResetBase
{
    [DbFact]
    public async Task I18nAuditWrites_AreStoredWithIpAddress_ForAllI18nOperations()
    {
        var baseConnectionString = IntegrationTestDatabase.GetConnectionString();
        using var db = new SqlSugarClientFactory(baseConnectionString).Create();
        await PrepareDatabaseAsync(db);

        db.Ado.ExecuteCommand("DELETE FROM sys_audit_log");

        var service = new I18nMessageService(new I18nMessageRepository(db));
        var now = new DateTimeOffset(2026, 6, 19, 0, 0, 0, TimeSpan.Zero);
        var messageKey = $"unittest.label.{DateTimeOffset.UtcNow.Ticks}";
        var actorUserId = Scalar<long>(db, "SELECT id FROM sys_user WHERE username = 'admin' LIMIT 1");
        if (actorUserId <= 0)
        {
            db.Ado.ExecuteCommand(
                "INSERT INTO sys_user (username, display_name, password_hash, status, is_super_admin, must_change_password, security_stamp, permission_version, created_at, updated_at, deleted_at) VALUES ('admin', 'admin', 'x', 'enabled', true, false, 'seed', 0, NOW(6), NOW(6), NULL)");
            actorUserId = Scalar<long>(db, "SELECT id FROM sys_user WHERE username = 'admin' LIMIT 1");
        }
        var context = new I18nRequestContext(actorUserId, "admin", "192.168.101.199", "i18n-integration", "it-audit", now);

        var created = await service.CreateAsync(
            new CreateI18nMessageRequest("en-US", "system", messageKey, "Hello", "Unit test message", "enabled"),
            context,
            CancellationToken.None);
        var createdMessageCount = Scalar<long>(
            db,
            "SELECT COUNT(1) FROM sys_i18n_message WHERE locale = @locale AND module = @module AND message_key = @messageKey",
            new SugarParameter("@locale", "en-US"),
            new SugarParameter("@module", "system"),
            new SugarParameter("@messageKey", messageKey));
        var allMessageCount = Scalar<long>(db, "SELECT COUNT(1) FROM sys_i18n_message");
        Assert.Equal(1, allMessageCount);
        Assert.Equal(1, createdMessageCount);
        var createdMessageId = Scalar<long>(
            db,
            "SELECT id FROM sys_i18n_message WHERE locale = @locale AND module = @module AND message_key = @messageKey LIMIT 1",
            new SugarParameter("@locale", "en-US"),
            new SugarParameter("@module", "system"),
            new SugarParameter("@messageKey", messageKey));
        Assert.True(createdMessageId > 0);
        Assert.True(created.Id > 0);

        await service.UpdateAsync(
            createdMessageId,
            new UpdateI18nMessageRequest("system", "Hello updated", "Unit test message", "enabled"),
            context,
            CancellationToken.None);

        await service.SwitchLocaleAsync(new SwitchAccountLocaleRequest("zh-CN"), context, CancellationToken.None);

        await service.DeleteAsync(createdMessageId, context, CancellationToken.None);

        Assert.Equal(1, Scalar<long>(db, "SELECT COUNT(1) FROM sys_audit_log WHERE action = 'create'"));
        Assert.Equal("192.168.101.199", ScalarString(db, "SELECT ip_address FROM sys_audit_log WHERE action = 'create' AND target_id = @targetId LIMIT 1", new SugarParameter("@targetId", createdMessageId)));
        Assert.Equal("192.168.101.199", ScalarString(db, "SELECT ip_address FROM sys_audit_log WHERE action = 'update' AND target_id = @targetId LIMIT 1", new SugarParameter("@targetId", createdMessageId)));
        Assert.Equal("192.168.101.199", ScalarString(db, "SELECT ip_address FROM sys_audit_log WHERE action = 'delete' AND target_id = @targetId LIMIT 1", new SugarParameter("@targetId", createdMessageId)));
        Assert.Equal("192.168.101.199", ScalarString(db, "SELECT ip_address FROM sys_audit_log WHERE action = 'switch-locale' LIMIT 1"));
    }

    private static T Scalar<T>(ISqlSugarClient db, string sql, params SugarParameter[] parameters)
    {
        var scalar = db.Ado.GetScalar(sql, parameters);
        if (scalar is null or DBNull)
        {
            return default!;
        }

        if (scalar is T value)
        {
            return value;
        }

        return (T)Convert.ChangeType(scalar, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string ScalarString(ISqlSugarClient db, string sql, params SugarParameter[] parameters)
    {
        var scalar = Scalar<string>(db, sql, parameters);
        Assert.NotNull(scalar);
        Assert.NotEqual(string.Empty, scalar);
        return scalar!;
    }
}
