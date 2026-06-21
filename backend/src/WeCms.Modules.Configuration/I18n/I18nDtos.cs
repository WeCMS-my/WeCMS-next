using WeCms.Shared;

namespace WeCms.Modules.Configuration.I18n;

public sealed record I18nMessageListQuery(int Page = 1, int PageSize = 20, string? Locale = null, string? Module = null, string? Keyword = null, string? Status = null);

public sealed record PublicI18nMessagesQuery(string Locale);

public sealed record I18nMessageSummaryDto(long Id, string Locale, string Module, string MessageKey, string MessageValue, string Status, DateTimeOffset UpdatedAt);

public sealed record I18nMessageDetailDto(long Id, string Locale, string Module, string MessageKey, string MessageValue, string? Remark, string Status, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

public sealed record CreateI18nMessageRequest(string Locale, string Module, string MessageKey, string MessageValue, string? Remark, string Status);

public sealed record UpdateI18nMessageRequest(string Module, string MessageValue, string? Remark, string Status);

public sealed record SwitchAccountLocaleRequest(string Locale);

public sealed record I18nMessagesResponse(string Locale, IReadOnlyDictionary<string, string> Messages);

public sealed record AccountI18nSwitchResponse(string Locale);

public sealed record I18nMutationResponse(long Id);

public sealed record I18nRequestContext(long ActorUserId, string ActorUsername, string Ip, string UserAgent, string TraceId, DateTimeOffset Now);

public interface II18nMessageService
{
    Task<PagedResult<I18nMessageSummaryDto>> ListAsync(I18nMessageListQuery query, CancellationToken cancellationToken);
    Task<I18nMessageDetailDto> GetAsync(long id, CancellationToken cancellationToken);
    Task<I18nMutationResponse> CreateAsync(CreateI18nMessageRequest request, I18nRequestContext context, CancellationToken cancellationToken);
    Task<I18nMutationResponse> UpdateAsync(long id, UpdateI18nMessageRequest request, I18nRequestContext context, CancellationToken cancellationToken);
    Task DeleteAsync(long id, I18nRequestContext context, CancellationToken cancellationToken);
    Task<I18nMessagesResponse> GetPublicMessagesAsync(PublicI18nMessagesQuery query, CancellationToken cancellationToken);
    Task<AccountI18nSwitchResponse> SwitchLocaleAsync(SwitchAccountLocaleRequest request, I18nRequestContext context, CancellationToken cancellationToken);
}

