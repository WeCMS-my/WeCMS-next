using SqlSugar;
using WeCms.Modules.Audit.SqlSugar.Entities;
using WeCms.Shared.Endpoints;

namespace WeCms.Modules.Audit.SqlSugar;

public sealed class SqlSugarAuditWriter : IAuditWriter
{
    private readonly ISqlSugarClient db;

    public SqlSugarAuditWriter(ISqlSugarClient db)
    {
        this.db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async ValueTask WriteAsync(AuditWriteRecord record, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(record);
        cancellationToken.ThrowIfCancellationRequested();

        var affectedRows = await db.Insertable(new AuditLogEntity
        {
            UserId = record.UserId,
            Username = Optional(record.Username, 64),
            Module = Required(record.Module, nameof(record.Module), 80),
            Resource = Required(record.Resource, nameof(record.Resource), 80),
            Action = Required(record.Action, nameof(record.Action), 80),
            TargetId = Optional(record.TargetId, 128),
            RequestMethod = Required(record.RequestMethod, nameof(record.RequestMethod), 16),
            RequestPath = Required(record.RequestPath, nameof(record.RequestPath), 160),
            IpAddress = Optional(record.IpAddress, 64),
            UserAgent = Optional(record.UserAgent, 500),
            TraceId = Optional(record.TraceId, 64),
            Result = record.Status.ToString(),
            Detail = Optional(record.Detail, 500) ?? string.Empty,
            CreatedAt = DateTime.UtcNow
        }).ExecuteCommandAsync();

        if (affectedRows != 1)
        {
            throw new InvalidOperationException("Audit log insert did not affect exactly one row.");
        }
    }

    private static string Required(string value, string name, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Audit field is required.", name);
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentException($"Audit field must be {maxLength} characters or fewer.", name);
        }

        return normalized;
    }

    private static string? Optional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentException($"Audit field must be {maxLength} characters or fewer.", nameof(value));
        }

        return normalized;
    }
}
