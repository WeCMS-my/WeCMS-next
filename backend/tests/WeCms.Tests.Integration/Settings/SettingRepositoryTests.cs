using SqlSugar;
using WeCms.Data.SqlSugar;
using WeCms.Modules.Configuration.Settings;
using WeCms.Modules.Configuration.SqlSugar.Repositories;
using WeCms.Shared.Security;

namespace WeCms.Tests.Integration.Settings;

[Collection(nameof(SharedMySqlCollection))]
public sealed class SettingRepositoryTests : PerTestDatabaseResetBase
{
    [DbFact]
    public async Task SettingRepository_UsesSettingTableAndWritesAuditAndSecurityEvent()
    {
        var baseConnectionString = IntegrationTestDatabase.GetConnectionString();

        using var db = new SqlSugarClientFactory(baseConnectionString).Create();
        await PrepareDatabaseWithSeedsAsync(db);

        var repository = new SettingRepository(db, new SecurityEventClassifier());
        var now = new DateTimeOffset(2026, 6, 21, 0, 0, 0, TimeSpan.Zero);

        var list = await repository.ListAsync(new SettingListCriteria(1, 20, "password", "security"), CancellationToken.None);
        var detail = await repository.GetAsync("security.passwordPepper", CancellationToken.None);

        Assert.Contains(list.Records, setting => setting.Key == "security.passwordPepper");
        Assert.NotNull(detail);
        Assert.True(detail.IsSensitive);

        await repository.UpdateAsync(
            new SettingUpdateRecord("security.passwordPepper", "protected-value", 1, now),
            CancellationToken.None);
        await repository.RecordAuditAsync(
            new SettingAuditRecord(1, "admin", "update-sensitive", "security.passwordPepper", "127.0.0.1", "integration-test", "trace-setting", "success", "Sensitive setting updated.", now),
            CancellationToken.None);
        await repository.RecordSecurityEventAsync(
            new SettingSecurityEventRecord("security.setting_changed", 1, "admin", "127.0.0.1", "warning", "Security setting security.passwordPepper changed.", now, "trace-setting-security"),
            CancellationToken.None);

        Assert.Equal("protected-value", Scalar<string>(db, "SELECT `value` FROM sys_setting WHERE `key` = 'security.passwordPepper' LIMIT 1"));
        Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM sys_audit_log WHERE resource = 'setting' AND target_id = 'security.passwordPepper'"));
        Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM sys_security_event WHERE trace_id = 'trace-setting-security' AND event_type = 'settings_sensitive_changed'"));
    }

    private static T Scalar<T>(ISqlSugarClient db, string sql, params SugarParameter[] parameters)
    {
        var scalar = db.Ado.GetScalar(sql, parameters);

        return scalar is T value
            ? value
            : (T)Convert.ChangeType(scalar, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
    }
}
