namespace WeCms.Modules.System.I18n;

public interface II18nService
{
    Task<List<I18nMessageItem>> ListAsync(string? locale, string? key, CancellationToken ct);
    Task<long> CreateAsync(CreateI18nRequest req, CancellationToken ct);
    Task UpdateAsync(long id, UpdateI18nRequest req, CancellationToken ct);
    Task DeleteAsync(long id, CancellationToken ct);
}
