 using WeCms.Shared.Contracts;
 
 namespace WeCms.Modules.System.Security;
 
 public sealed class SecurityService(IDbConnectionFactory db) : ISecurityService
{
    // Sort is hardcoded (id DESC) — no user input, no injection risk.
    public async Task<(IReadOnlyList<SecurityEventItem> Items, long Total)> ListEventsAsync(int page, int size, string? type, CancellationToken ct)
     { await using var c = await db.OpenAsync(ct); var items = await c.QueryAsync<SecurityEventItem>(new CommandDefinition("SELECT id, event_type, severity, user_id, username, ip, detail, created_at FROM sys_security_event WHERE (@T IS NULL OR event_type=@T) ORDER BY id DESC LIMIT @L OFFSET @O", new { T = type, L = Math.Min(size, 100), O = (page - 1) * size }, cancellationToken: ct)); var total = await c.ExecuteScalarAsync<long>(new CommandDefinition("SELECT COUNT(1) FROM sys_security_event WHERE (@T IS NULL OR event_type=@T)", new { T = type }, cancellationToken: ct)); return (items.AsList(), total); }
 }
