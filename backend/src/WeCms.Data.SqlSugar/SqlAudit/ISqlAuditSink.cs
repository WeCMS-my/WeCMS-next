namespace WeCms.Data.SqlSugar;

public interface ISqlAuditSink
{
    void Write(SqlAuditRecord record);
}

public sealed class NullSqlAuditSink : ISqlAuditSink
{
    public void Write(SqlAuditRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
    }
}
