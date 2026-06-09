namespace WeCms.Modules.System.Security;

public interface ISecurityService
{
    Task<(IReadOnlyList<SecurityEventItem> Items, long Total)> ListEventsAsync(int page, int size, string? type, CancellationToken ct);
}
