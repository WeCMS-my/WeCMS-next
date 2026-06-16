using WeCms.Shared;

namespace WeCms.Modules.System.Posts;

public sealed record PostListQuery(int Page = 1, int PageSize = 20, string? Keyword = null, string? Status = null);

public sealed record PostSummaryDto(long Id, string Code, string Name, int SortOrder, string Status, DateTimeOffset CreatedAt);

public sealed record PostDetailDto(long Id, string Code, string Name, int SortOrder, string Status, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

public sealed record CreatePostRequest(string Code, string Name, int SortOrder, string Status);

public sealed record UpdatePostRequest(string Name, int SortOrder, string Status);

public sealed record PostMutationResponse(long Id);

public interface IPostService
{
    Task<PagedResult<PostSummaryDto>> ListAsync(PostListQuery query, CancellationToken cancellationToken);
    Task<PostDetailDto> GetAsync(long id, CancellationToken cancellationToken);
    Task<PostMutationResponse> CreateAsync(CreatePostRequest request, PostRequestContext context, CancellationToken cancellationToken);
    Task<PostMutationResponse> UpdateAsync(long id, UpdatePostRequest request, PostRequestContext context, CancellationToken cancellationToken);
    Task DeleteAsync(long id, PostRequestContext context, CancellationToken cancellationToken);
    Task EnableAsync(long id, PostRequestContext context, CancellationToken cancellationToken);
    Task DisableAsync(long id, PostRequestContext context, CancellationToken cancellationToken);
}
