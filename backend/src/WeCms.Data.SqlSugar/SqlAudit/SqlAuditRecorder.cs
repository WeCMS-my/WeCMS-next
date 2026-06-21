using System.Security.Cryptography;
using System.Text;
using SqlSugar;

namespace WeCms.Data.SqlSugar;

public sealed class SqlAuditRecorder
{
    private static readonly AsyncLocal<bool> IsWritingAudit = new();
    private readonly SqlAuditOptions _options;
    private readonly ISqlAuditSink _sink;
    private readonly ISqlAuditContextAccessor _contextAccessor;
    private readonly SqlAuditRedactor _redactor;
    private readonly Func<DateTime> _utcNow;

    public SqlAuditRecorder(
        SqlAuditOptions options,
        ISqlAuditSink sink,
        ISqlAuditContextAccessor contextAccessor)
        : this(options, sink, contextAccessor, () => DateTime.UtcNow)
    {
    }

    internal SqlAuditRecorder(
        SqlAuditOptions options,
        ISqlAuditSink sink,
        ISqlAuditContextAccessor contextAccessor,
        Func<DateTime> utcNow)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _sink = sink ?? throw new ArgumentNullException(nameof(sink));
        _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
        _redactor = new SqlAuditRedactor();
        _utcNow = utcNow ?? throw new ArgumentNullException(nameof(utcNow));
    }

    public void RecordExecuted(
        string connectionName,
        string sql,
        IReadOnlyList<SugarParameter> parameters,
        TimeSpan elapsed,
        int? affectedRows = null)
    {
        var elapsedMs = Convert.ToInt64(Math.Max(0, elapsed.TotalMilliseconds));
        var isSlowSql = elapsedMs >= _options.SlowSqlThresholdMilliseconds;
        if (!_options.CaptureAllSql && !isSlowSql)
        {
            return;
        }

        WriteRecord(connectionName, sql, parameters, elapsedMs, affectedRows, isSlowSql, errorMessage: null);
    }

    public void RecordFailed(
        string connectionName,
        string sql,
        IReadOnlyList<SugarParameter> parameters,
        Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        WriteRecord(
            connectionName,
            sql,
            parameters,
            elapsedMs: 0,
            affectedRows: null,
            isSlowSql: false,
            errorMessage: exception.Message);
    }

    private void WriteRecord(
        string connectionName,
        string sql,
        IReadOnlyList<SugarParameter> parameters,
        long elapsedMs,
        int? affectedRows,
        bool isSlowSql,
        string? errorMessage)
    {
        if (IsWritingAudit.Value)
        {
            return;
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(connectionName);
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);
        ArgumentNullException.ThrowIfNull(parameters);

        var context = _contextAccessor.Current;
        var record = new SqlAuditRecord(
            context.TraceId,
            context.UserId,
            context.Username,
            context.TenantId,
            connectionName,
            context.RepositoryName,
            GetOperationType(sql),
            ComputeHash(sql),
            sql,
            _redactor.Redact(parameters),
            elapsedMs,
            affectedRows,
            isSlowSql,
            errorMessage,
            _utcNow());

        try
        {
            IsWritingAudit.Value = true;
            _sink.Write(record);
        }
        finally
        {
            IsWritingAudit.Value = false;
        }
    }

    private static string GetOperationType(string sql)
    {
        var trimmed = sql.TrimStart();
        if (trimmed.Length == 0)
        {
            return "UNKNOWN";
        }

        var firstWhitespace = trimmed.IndexOfAny([' ', '\r', '\n', '\t']);
        var operation = firstWhitespace < 0 ? trimmed : trimmed[..firstWhitespace];
        return operation.ToUpperInvariant();
    }

    private static string ComputeHash(string sql)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(sql));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
