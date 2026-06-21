using SqlSugar;

namespace WeCms.Data.SqlSugar;

public sealed class SqlSugarSqlAuditRegistrar : ISqlSugarAuditRegistrar
{
    private readonly SqlAuditRecorder _recorder;
    private readonly IReadOnlyList<ISqlTimingRecorder> _timingRecorders;
    private readonly SqlAuditRedactor _redactor = new();

    public SqlSugarSqlAuditRegistrar(
        SqlAuditRecorder recorder,
        IEnumerable<ISqlTimingRecorder>? timingRecorders = null)
    {
        _recorder = recorder ?? throw new ArgumentNullException(nameof(recorder));
        _timingRecorders = (timingRecorders ?? []).ToArray();
    }

    public void Register(SqlSugarScopeProvider db)
    {
        ArgumentNullException.ThrowIfNull(db);

        var connectionName = db.CurrentConnectionConfig.ConfigId?.ToString() ?? "default";

        db.Aop.OnLogExecuted = (sql, parameters) =>
        {
            var sugarParameters = ToSugarParameters(parameters);
            _recorder.RecordExecuted(
                connectionName,
                sql,
                sugarParameters,
                db.Ado.SqlExecutionTime);
            RecordSqlTiming(connectionName, sql, sugarParameters, db.Ado.SqlExecutionTime, exception: null);
        };

        db.Aop.OnError = exception =>
        {
            var sugarParameters = ToSugarParameters(exception.Parametres);
            _recorder.RecordFailed(
                connectionName,
                exception.Sql,
                sugarParameters,
                exception);
            RecordSqlTiming(connectionName, exception.Sql, sugarParameters, TimeSpan.Zero, exception);
        };
    }

    private void RecordSqlTiming(
        string connectionName,
        string sql,
        IReadOnlyList<SugarParameter> parameters,
        TimeSpan elapsed,
        Exception? exception)
    {
        if (_timingRecorders.Count == 0)
        {
            return;
        }

        var record = new SqlTimingRecord(
            connectionName,
            OperationType(sql),
            sql,
            _redactor.Redact(parameters),
            elapsed);
        foreach (var timingRecorder in _timingRecorders)
        {
            if (exception is null)
            {
                timingRecorder.RecordExecuted(record);
            }
            else
            {
                timingRecorder.RecordFailed(record, exception);
            }
        }
    }

    private static IReadOnlyList<SugarParameter> ToSugarParameters(object? parameters)
    {
        return parameters switch
        {
            null => [],
            SugarParameter[] sugarParameters => sugarParameters,
            IReadOnlyList<SugarParameter> sugarParameters => sugarParameters,
            IEnumerable<SugarParameter> sugarParameters => sugarParameters.ToArray(),
            _ => []
        };
    }

    private static string OperationType(string sql)
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
}
