 using WeCms.Shared.Contracts;
 
 namespace WeCms.Modules.System.Settings;
 
 public sealed class SettingService(IDbConnectionFactory db, IClock clock, IAuditWriter audit) : ISettingService
{
    private static readonly HashSet<string> SensitiveKeys = new(StringComparer.OrdinalIgnoreCase) { "smtp_pass", "auth_key", "jwt_secret", "sms_secret" };
 
     public async Task<List<SettingItem>> GetAllAsync(CancellationToken ct)
     {
         await using var conn = await db.OpenAsync(ct);
         var rows = await conn.QueryAsync<SettingRow>(new CommandDefinition("SELECT `key`, `value`, `group`, description FROM sys_setting ORDER BY `group`, `key`", cancellationToken: ct));
         return rows.Select(r => new SettingItem(r.Key, SensitiveKeys.Contains(r.Key) ? "***" : r.Value, r.Group, r.Description, SensitiveKeys.Contains(r.Key))).ToList();
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
