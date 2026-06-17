using WeCms.Modules.System.Posts;
using WeCms.Shared;

namespace WeCms.Tests.Unit.Posts;

public sealed class PostServiceTests
{
    [Fact]
    public async Task ListAsync_RejectsPageSizeGreaterThanOneHundred()
    {
        var service = new PostService(new FakePostRepository());

        var exception = await Assert.ThrowsAsync<DomainException>(() => service.ListAsync(new PostListQuery(PageSize: 101), CancellationToken.None));

        Assert.Equal(ApiCodes.ValidationError, exception.Code);
    }

    [Fact]
    public async Task CreateAsync_RejectsDuplicateCode()
    {
        var service = new PostService(new FakePostRepository { CodeExists = true });

        var exception = await Assert.ThrowsAsync<DomainException>(() => service.CreateAsync(new CreatePostRequest("dev", "Developer", 1, "enabled"), Context(), CancellationToken.None));

        Assert.Equal(ApiCodes.Conflict, exception.Code);
    }

    [Fact]
    public async Task DeleteAsync_RejectsPostAssignedToUsers()
    {
        var service = new PostService(new FakePostRepository { HasUsers = true });

        var exception = await Assert.ThrowsAsync<DomainException>(() => service.DeleteAsync(1, Context(), CancellationToken.None));

        Assert.Equal(ApiCodes.BusinessError, exception.Code);
    }

    private static PostRequestContext Context() => new(1, "admin", "192.168.101.199", "unit-test", "trace", new DateTimeOffset(2026, 6, 16, 0, 0, 0, TimeSpan.Zero));

    private sealed class FakePostRepository : IPostRepository
    {
        public bool CodeExists { get; init; }
        public bool HasUsers { get; init; }

        public Task<PagedResult<PostSummaryDto>> ListAsync(PostListCriteria criteria, CancellationToken cancellationToken) => Task.FromResult(new PagedResult<PostSummaryDto>([], criteria.Page, criteria.PageSize, 0));
        public Task<PostDetailDto?> GetAsync(long id, CancellationToken cancellationToken) => Task.FromResult<PostDetailDto?>(new PostDetailDto(id, "dev", "Developer", 1, "enabled", DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch));
        public Task<bool> CodeExistsAsync(string code, long? exceptPostId, CancellationToken cancellationToken) => Task.FromResult(CodeExists);
        public Task<bool> HasUsersAsync(long id, CancellationToken cancellationToken) => Task.FromResult(HasUsers);
        public Task<long> CreateAsync(PostCreateRecord record, CancellationToken cancellationToken) => Task.FromResult(2L);
        public Task UpdateAsync(PostUpdateRecord record, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SoftDeleteAsync(long id, DateTimeOffset now, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SetStatusAsync(long id, string status, DateTimeOffset now, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task RecordAuditAsync(PostAuditRecord record, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
