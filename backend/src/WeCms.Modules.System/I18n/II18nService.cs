namespace WeCms.Modules.System.I18n;

public interface II18nService
{
    Task<(IReadOnlyList<I18nMessageItem> Items, long Total)> ListAsync(string? locale, string? key, int page, int size, CancellationToken ct);
    Task<long> CreateAsync(CreateI18nRequest req, CancellationToken ct);
    Task UpdateAsync(long id, UpdateI18nRequest req, CancellationToken ct);
    Task DeleteAsync(long id, CancellationToken ct);
}
