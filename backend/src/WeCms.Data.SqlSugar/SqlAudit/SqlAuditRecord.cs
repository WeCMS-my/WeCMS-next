namespace WeCms.Data.SqlSugar;

public sealed record SqlAuditRecord(
    string? TraceId,
    long? UserId,
    string? Username,
    long? TenantId,
    string ConnectionName,
    string? RepositoryName,
    string OperationType,
    string SqlHash,
    string SqlTemplate,
    IReadOnlyDictionary<string, string?> ParametersRedacted,
    long ElapsedMs,
    int? AffectedRows,
    bool IsSlowSql,
    string? ErrorMessage,
    DateTime CreatedAt);
