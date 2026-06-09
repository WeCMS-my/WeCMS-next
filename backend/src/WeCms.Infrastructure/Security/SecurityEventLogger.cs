using WeCms.Shared.Contracts;
using Dapper;
using System.Data.Common;

namespace WeCms.Infrastructure.Security;

public sealed class SecurityEventLogger(IDbConnectionFactory db, IClock clock) : ISecurityEventLogger
{
    public async Task LogAsync(string eventType, string severity, long? userId, string? username, string? ip, string? detail, CancellationToken ct)
    {
        await using var conn = await db.OpenAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition("""
            INSERT INTO sys_security_event (event_type, severity, user_id, username, ip, detail, created_at)
            VALUES (@Type, @Severity, @UserId, @Username, @Ip, @Detail, @Now)
            """,
            new { Type = eventType, Severity = severity, UserId = userId, Username = username,
                  Ip = ip, Detail = detail, Now = clock.UtcNow.DateTime }, cancellationToken: ct));
    }
}