using WeCms.Shared;

namespace WeCms.Modules.System.Posts;

public interface IPostRepository
{
    Task<PagedResult<PostSummaryDto>> ListAsync(PostListCriteria criteria, CancellationToken cancellationToken);
    Task<PostDetailDto?> GetAsync(long id, CancellationToken cancellationToken);
    Task<bool> CodeExistsAsync(string code, long? exceptPostId, CancellationToken cancellationToken);
    Task<bool> HasUsersAsync(long id, CancellationToken cancellationToken);
    Task<long> CreateAsync(PostCreateRecord record, CancellationToken cancellationToken);
    Task UpdateAsync(PostUpdateRecord record, CancellationToken cancellationToken);
    Task SoftDeleteAsync(long id, DateTimeOffset now, CancellationToken cancellationToken);
    Task SetStatusAsync(long id, string status, DateTimeOffset now, CancellationToken cancellationToken);
    Task RecordAuditAsync(PostAuditRecord record, CancellationToken cancellationToken);
}
