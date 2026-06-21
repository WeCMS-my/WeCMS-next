namespace WeCms.Data.SqlSugar;

public interface ISqlTimingRecorder
{
    void RecordExecuted(SqlTimingRecord record);

    void RecordFailed(SqlTimingRecord record, Exception exception);
}

public sealed record SqlTimingRecord(
    string ConnectionName,
    string OperationType,
    string SqlTemplate,
    IReadOnlyDictionary<string, string?> ParametersRedacted,
    TimeSpan Elapsed);
