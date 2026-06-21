using WeCms.Shared;

namespace WeCms.Modules.Organization.Positions;

public sealed record PositionListQuery(int Page = 1, int PageSize = 20, string? Keyword = null, string? Status = null);

public sealed record PositionSummaryDto(long Id, string Code, string Name, int SortOrder, string Status, DateTimeOffset CreatedAt);

public sealed record PositionDetailDto(long Id, string Code, string Name, int SortOrder, string Status, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

public sealed record CreatePositionRequest(string Code, string Name, int SortOrder, string Status);

public sealed record UpdatePositionRequest(string Name, int SortOrder, string Status);

public sealed record PositionMutationResponse(long Id);

public interface IPositionService
{
    Task<PagedResult<PositionSummaryDto>> ListAsync(PositionListQuery query, CancellationToken cancellationToken);
    Task<PositionDetailDto> GetAsync(long id, CancellationToken cancellationToken);
    Task<PositionMutationResponse> CreateAsync(CreatePositionRequest request, PositionRequestContext context, CancellationToken cancellationToken);
    Task<PositionMutationResponse> UpdateAsync(long id, UpdatePositionRequest request, PositionRequestContext context, CancellationToken cancellationToken);
    Task DeleteAsync(long id, PositionRequestContext context, CancellationToken cancellationToken);
    Task EnableAsync(long id, PositionRequestContext context, CancellationToken cancellationToken);
    Task DisableAsync(long id, PositionRequestContext context, CancellationToken cancellationToken);
}
