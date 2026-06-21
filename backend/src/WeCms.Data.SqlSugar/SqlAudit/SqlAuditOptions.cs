namespace WeCms.Data.SqlSugar;

public sealed record SqlAuditOptions
{
    public const int DefaultSlowSqlThresholdMilliseconds = 500;

    public int SlowSqlThresholdMilliseconds { get; init; } = DefaultSlowSqlThresholdMilliseconds;

    public bool CaptureAllSql { get; init; }
}
