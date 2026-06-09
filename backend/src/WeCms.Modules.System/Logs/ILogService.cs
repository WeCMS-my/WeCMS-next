namespace WeCms.Modules.System.Logs;

public interface ILogService
{
    Task<(IReadOnlyList<LoginLogItem> Items, long Total)> GetLoginLogsAsync(int page, int size, string? status, CancellationToken ct);
    Task<(IReadOnlyList<AuditLogItem> Items, long Total)> GetAuditLogsAsync(int page, int size, string? module, CancellationToken ct);
}
