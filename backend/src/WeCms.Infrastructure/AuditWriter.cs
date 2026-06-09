using WeCms.Shared.Contracts;
using Dapper;
using System.Data.Common;

namespace WeCms.Infrastructure;

public sealed class AuditWriter(IDbConnectionFactory db, IClock clock) : IAuditWriter
{
    public async Task LogAsync(string module, string action, long? userId, string? username, string? ip, string? userAgent, int? statusCode, string result, CancellationToken ct)
        => await LogAsync(module, action, userId, username, ip, userAgent, statusCode, result, null, ct);

    public async Task LogAsync(string module, string action, long? userId, string? username, string? ip, string? userAgent, int? statusCode, string result, string? errorMessage, CancellationToken ct)
    {
        await using var conn = await db.OpenAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition("""
            INSERT INTO sys_audit_log (user_id, username, module, action, ip, user_agent, request_body_summary, status_code, result, error_message, created_at)
            VALUES (@UserId, @Username, @Module, @Action, @Ip, @UserAgent, NULL, @StatusCode, @Result, @Error, @Now)
            """,
            new { UserId = userId, Username = username ?? "system", Module = module, Action = action,
                  Ip = ip, UserAgent = userAgent, StatusCode = statusCode, Result = result, Error = errorMessage,
                  Now = clock.UtcNow.DateTime }, cancellationToken: ct));
    }
}
