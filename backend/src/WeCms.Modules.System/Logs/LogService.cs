 using WeCms.Shared.Contracts;
 
 namespace WeCms.Modules.System.Logs;
 
 public sealed class LogService(IDbConnectionFactory db) : ILogService
 {
     public async Task<(IReadOnlyList<LoginLogItem> Items, long Total)> GetLoginLogsAsync(int page, int size, string? status, CancellationToken ct)
     { await using var c = await db.OpenAsync(ct); var items = await c.QueryAsync<LoginLogItem>(new CommandDefinition("SELECT id, user_id, username, status, ip, created_at FROM sys_login_log WHERE (@S IS NULL OR status=@S) ORDER BY id DESC LIMIT @L OFFSET @O", new { S = status, L = Math.Min(size,100), O = (page-1)*size }, cancellationToken: ct)); var total = await c.ExecuteScalarAsync<long>(new CommandDefinition("SELECT COUNT(1) FROM sys_login_log", cancellationToken: ct)); return (items.AsList(), total); }
 
     public async Task<(IReadOnlyList<AuditLogItem> Items, long Total)> GetAuditLogsAsync(int page, int size, string? module, CancellationToken ct)
     { await using var c = await db.OpenAsync(ct); var items = await c.QueryAsync<AuditLogItem>(new CommandDefinition("SELECT id, user_id, username, module, action, ip, created_at FROM sys_audit_log WHERE (@M IS NULL OR module=@M) ORDER BY id DESC LIMIT @L OFFSET @O", new { M = module, L = Math.Min(size,100), O = (page-1)*size }, cancellationToken: ct)); var total = await c.ExecuteScalarAsync<long>(new CommandDefinition("SELECT COUNT(1) FROM sys_audit_log", cancellationToken: ct)); return (items.AsList(), total); }
 }
