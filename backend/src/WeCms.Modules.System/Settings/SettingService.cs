using Dapper;
using WeCms.Shared.Contracts;

namespace WeCms.Modules.System.Settings;

public sealed class SettingService(IDbConnectionFactory db, IClock clock, IAuditWriter audit) : ISettingService
{
    private static readonly HashSet<string> SensitiveKeys = new(StringComparer.OrdinalIgnoreCase) { "smtp_pass", "auth_key", "jwt_secret", "sms_secret", "jwt_signing_key", "db_password", "smtp_user", "sms_secret_key" };

    public async Task<(IReadOnlyList<SettingItem> Items, long Total)> ListAsync(int page, int size, CancellationToken ct)
    {
        await using var conn = await db.OpenAsync(ct);
        size = Math.Min(size, 100);
        var rows = await conn.QueryAsync<SettingRow>(new CommandDefinition(
            "SELECT `key`, `value`, `group`, description FROM sys_setting ORDER BY `group`, `key` LIMIT @L OFFSET @O",
            new { L = size, O = (page - 1) * size }, cancellationToken: ct));
        var items = rows.Select(r => new SettingItem(r.Key, SensitiveKeys.Contains(r.Key) ? "***" : r.Value, r.Group, r.Description, SensitiveKeys.Contains(r.Key))).ToList();
        var total = await conn.ExecuteScalarAsync<long>(new CommandDefinition(
            "SELECT COUNT(1) FROM sys_setting", cancellationToken: ct));
        return (items, total);
    }

    public async Task UpdateAsync(string key, string value, CancellationToken ct)
    {
        if (SensitiveKeys.Contains(key) && value == "***")
            throw new InvalidOperationException("Cannot update with redacted value");
        await using var conn = await db.OpenAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition(
            "INSERT INTO sys_setting (`key`,`value`,updated_at) VALUES (@K,@V,@Now) ON DUPLICATE KEY UPDATE `value`=@V, updated_at=@Now",
            new { K = key, V = value, Now = clock.UtcNow.DateTime }, cancellationToken: ct));
        await audit.LogAsync("system", "setting:update", null, null, null, null, 200, "success", ct);
    }

    private sealed record SettingRow(string Key, string Value, string Group, string Description);
}
