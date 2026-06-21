using WeCms.Shared;

namespace WeCms.Modules.Security.Events;

public sealed class SecurityEventService : ISecurityEventService
{
    private const int MaxPageSize = 100;
    private readonly ISecurityEventRepository _repository;

    public SecurityEventService(ISecurityEventRepository repository)
    {
        _repository = repository;
    }

    public Task<PagedResult<SecurityEventSummaryDto>> ListSecurityEventsAsync(SecurityEventListQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page <= 0 ? throw Validation("page must be greater than or equal to 1.") : query.Page;
        var pageSize = query.PageSize is <= 0 or > MaxPageSize ? throw Validation($"pageSize must be between 1 and {MaxPageSize}.") : query.PageSize;
        if (query.From is not null && query.To is not null && query.From > query.To)
        {
            throw Validation("from must be earlier than or equal to to.");
        }

        return _repository.ListSecurityEventsAsync(
            new SecurityEventListCriteria(
                page,
                pageSize,
                NormalizeOptional(query.EventType, 80),
                NormalizeOptional(query.Severity, 32),
                NormalizeOptional(query.User, 64),
                NormalizeOptional(query.Ip, 64),
                query.From,
                query.To),
            cancellationToken);
    }

    public async Task<SecurityEventDetailDto> GetSecurityEventAsync(long id, CancellationToken cancellationToken)
    {
        return await _repository.GetSecurityEventAsync(id, cancellationToken) ?? throw new DomainException(ApiCodes.NotFound, "Security event was not found.");
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        return normalized.Length <= maxLength ? normalized : throw Validation($"value must be {maxLength} characters or fewer.");
    }

    private static DomainException Validation(string message) => new(ApiCodes.ValidationError, message);
}
