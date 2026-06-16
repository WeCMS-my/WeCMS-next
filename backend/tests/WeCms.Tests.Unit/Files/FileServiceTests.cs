using WeCms.Modules.System.Files;
using WeCms.Shared;

namespace WeCms.Tests.Unit.Files;

public sealed class FileServiceTests
{
    [Fact]
    public async Task CreateAsync_RejectsFileLargerThanTenMiB()
    {
        var service = new FileService(new FakeFileRepository());

        var exception = await Assert.ThrowsAsync<DomainException>(() => service.CreateAsync(new CreateFileRequest("a.pdf", "application/pdf", 10 * 1024 * 1024 + 1, "a".PadLeft(64, '0')), Context(), CancellationToken.None));

        Assert.Equal(ApiCodes.ValidationError, exception.Code);
    }

    [Fact]
    public async Task CreateAsync_RejectsDisallowedMimeType()
    {
        var service = new FileService(new FakeFileRepository());

        var exception = await Assert.ThrowsAsync<DomainException>(() => service.CreateAsync(new CreateFileRequest("a.exe", "application/octet-stream", 100, "a".PadLeft(64, '0')), Context(), CancellationToken.None));

        Assert.Equal(ApiCodes.ValidationError, exception.Code);
    }

    [Fact]
    public async Task CreateAsync_RejectsDisallowedExtension()
    {
        var service = new FileService(new FakeFileRepository());

        var exception = await Assert.ThrowsAsync<DomainException>(() => service.CreateAsync(new CreateFileRequest("a.exe", "application/pdf", 100, "a".PadLeft(64, '0')), Context(), CancellationToken.None));

        Assert.Equal(ApiCodes.ValidationError, exception.Code);
    }

    [Fact]
    public async Task GetAsync_DoesNotExposeObjectKey()
    {
        var service = new FileService(new FakeFileRepository());

        var detail = await service.GetAsync(1, CancellationToken.None);

        Assert.Equal("a.pdf", detail.OriginalName);
    }

    private static FileRequestContext Context() => new(1, "admin", "127.0.0.1", "unit-test", "trace", new DateTimeOffset(2026, 6, 16, 0, 0, 0, TimeSpan.Zero));

    private sealed class FakeFileRepository : IFileRepository
    {
        public Task<PagedResult<FileSummaryDto>> ListAsync(FileListCriteria criteria, CancellationToken cancellationToken) => Task.FromResult(new PagedResult<FileSummaryDto>([], criteria.Page, criteria.PageSize, 0));
        public Task<FileDetailDto?> GetAsync(long id, CancellationToken cancellationToken) => Task.FromResult<FileDetailDto?>(new FileDetailDto(id, "a.pdf", ".pdf", "application/pdf", 100, "a".PadLeft(64, '0'), "active", 1, DateTimeOffset.UnixEpoch));
        public Task<long> CreateAsync(FileCreateRecord record, CancellationToken cancellationToken) => Task.FromResult(1L);
        public Task SoftDeleteAsync(long id, DateTimeOffset now, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task RecordAuditAsync(FileAuditRecord record, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
