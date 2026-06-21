using WeCms.Shared;

namespace WeCms.Modules.Configuration.I18n;

public interface II18nMessageRepository
{
    Task<PagedResult<I18nMessageSummaryDto>> ListAsync(I18nMessageListCriteria criteria, CancellationToken cancellationToken);
    Task<I18nMessageDetailDto?> GetAsync(long id, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(string locale, string messageKey, long? exceptId, CancellationToken cancellationToken);
    Task<long> CreateAsync(I18nMessageCreateRecord record, CancellationToken cancellationToken);
    Task UpdateAsync(I18nMessageUpdateRecord record, CancellationToken cancellationToken);
    Task SoftDeleteAsync(long id, DateTimeOffset now, CancellationToken cancellationToken);
    Task<IReadOnlyList<I18nPublicMessageRecord>> ListPublicMessagesAsync(string locale, string status, CancellationToken cancellationToken);
    Task RecordAuditAsync(I18nAuditRecord record, CancellationToken cancellationToken);
}

