using SqlSugar;
using WeCms.Modules.Security;
using WeCms.Shared.Security;

namespace WeCms.Modules.Security.SqlSugar.Repositories;

public sealed class RateLimitSecurityEventRepository : IRateLimitSecurityEventRepository
{
    private readonly ISqlSugarClient _db;
    private readonly ISecurityEventClassifier _classifier;

    public RateLimitSecurityEventRepository(ISqlSugarClient db, ISecurityEventClassifier classifier)
    {
        _db = db;
        _classifier = classifier;
    }

    public async Task RecordHitAsync(RateLimitSecurityEventRecord record, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var classification = _classifier.Classify(record.EventType, record.TraceId);

        var insertedRows = await _db.Ado.ExecuteCommandAsync(
            """
            INSERT INTO sys_security_event (event_type, user_id, username, ip, severity, source, message, trace_id, created_at)
            VALUES (@eventType, @userId, @username, @ip, @severity, @source, @message, @traceId, @createdAt)
            """,
            new SugarParameter("@eventType", classification.EventType),
            new SugarParameter("@userId", record.UserId),
            new SugarParameter("@username", record.Username),
            new SugarParameter("@ip", record.Ip),
            new SugarParameter("@severity", classification.Severity),
            new SugarParameter("@source", classification.Source),
            new SugarParameter("@message", record.Message),
            new SugarParameter("@traceId", classification.TraceId),
            new SugarParameter("@createdAt", record.CreatedAt.UtcDateTime));

        if (insertedRows != 1)
        {
            throw new InvalidOperationException("Failed to record rate limit security event.");
        }
    }
}
