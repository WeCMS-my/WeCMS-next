using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using WeCms.Modules.System.Files;
using WeCms.Shared;

namespace WeCms.Tests.Unit.Files;

public sealed class FileServiceTests
{
    [Fact]
    public async Task CreateAsync_RejectsFileLargerThanTenMiB()
    {
        var service = new FileService(new FakeFileRepository(), new FakeFileStorage(), new FakeObjectKeyGenerator());
        var oversizedFile = CreateFormFile("a.pdf", "application/pdf", new byte[(int)(10 * 1024 * 1024 + 1)]);

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            service.CreateAsync(
                new CreateFileRequest("a.pdf", "application/pdf", oversizedFile.Length, "a".PadLeft(64, '0')),
                oversizedFile,
                Context(),
                CancellationToken.None));

        Assert.Equal(ApiCodes.ValidationError, exception.Code);
    }

    [Fact]
    public async Task CreateAsync_RejectsDisallowedMimeType()
    {
        var service = new FileService(new FakeFileRepository(), new FakeFileStorage(), new FakeObjectKeyGenerator());
        var file = CreateFormFile("a.pdf", "application/pdf", [0x25, 0x50, 0x44, 0x46, 0x2D]);

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            service.CreateAsync(
                new CreateFileRequest("a.exe", "application/octet-stream", file.Length, "a".PadLeft(64, '0')),
                file,
                Context(),
                CancellationToken.None));

        Assert.Equal(ApiCodes.ValidationError, exception.Code);
    }

    [Fact]
    public async Task CreateAsync_RejectsDisallowedExtension()
    {
        var service = new FileService(new FakeFileRepository(), new FakeFileStorage(), new FakeObjectKeyGenerator());
        var file = CreateFormFile("a.exe", "text/plain", [0x41, 0x42, 0x43]);

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            service.CreateAsync(
                new CreateFileRequest("a.exe", "application/pdf", file.Length, "a".PadLeft(64, '0')),
                file,
                Context(),
                CancellationToken.None));

        Assert.Equal(ApiCodes.ValidationError, exception.Code);
    }

    [Fact]
    public async Task CreateAsync_RejectsSha256Mismatch()
    {
        var service = new FileService(new FakeFileRepository(), new FakeFileStorage(), new FakeObjectKeyGenerator());
        var file = CreateFormFile("a.pdf", "application/pdf", [0x25, 0x50, 0x44, 0x46, 0x2D]);

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            service.CreateAsync(
                new CreateFileRequest("a.pdf", "application/pdf", file.Length, "b".PadLeft(64, '0')),
                file,
                Context(),
                CancellationToken.None));

        Assert.Equal(ApiCodes.ValidationError, exception.Code);
    }

    [Fact]
    public async Task CreateAsync_CreatesMetadataWhenContentMatches()
    {
        var repository = new RecordingFileRepository();
        var service = new FileService(repository, new FakeFileStorage(), new FakeObjectKeyGenerator());
        var content = Encoding.UTF8.GetBytes("sample content");
        var file = CreateFormFile("upload.pdf", "application/pdf", content);
        var sha = ComputeSha256(content);

        var result = await service.CreateAsync(
            new CreateFileRequest("upload.pdf", "application/pdf", content.Length, sha),
            file,
            Context(),
            CancellationToken.None);

        Assert.Equal(1L, result.Id);
        Assert.NotNull(repository.Recorded);
        Assert.Equal("upload.pdf", repository.Recorded!.OriginalName);
        Assert.Equal(".pdf", repository.Recorded!.FileExt);
        Assert.Equal("application/pdf", repository.Recorded!.MimeType);
        Assert.Equal(content.Length, repository.Recorded!.SizeBytes);
        Assert.Equal(sha, repository.Recorded!.Sha256);
        Assert.Equal("active", repository.Recorded!.Status);
    }

    [Fact]
    public async Task GetAsync_DoesNotExposeObjectKey()
    {
        var service = new FileService(new FakeFileRepository(), new FakeFileStorage(), new FakeObjectKeyGenerator());

        var detail = await service.GetAsync(1, CancellationToken.None);

        Assert.Equal("a.pdf", detail.OriginalName);
    }

    private static FileRequestContext Context() => new(1, "admin", "192.168.101.199", "unit-test", "trace", new DateTimeOffset(2026, 6, 16, 0, 0, 0, TimeSpan.Zero));
    private static IFormFile CreateFormFile(string fileName, string contentType, byte[] content) => new FormFile(new MemoryStream(content), 0, content.Length, "file", fileName)
    {
        Headers = new HeaderDictionary()
    };

    [Fact]
    public async Task GetDownloadPayloadAsync_ReturnsFileStream()
    {
        var service = new FileService(new FakeFileRepository("active"), new FakeFileStorage(), new FakeObjectKeyGenerator());

        var payload = await service.GetDownloadPayloadAsync(1, false, Context(), CancellationToken.None);

        Assert.Equal("a.pdf", payload.FileName);
        Assert.Equal("application/pdf", payload.ContentType);
        Assert.Equal(5, payload.SizeBytes);
        using var memory = new MemoryStream();
        await payload.Content.CopyToAsync(memory, CancellationToken.None);
        Assert.Equal(5, memory.Length);
    }

    [Theory]
    [InlineData("disabled")]
    [InlineData("deleted")]
    public async Task GetDownloadPayloadAsync_ReturnsNotFoundWhenStatusIsNotActive(string status)
    {
        var service = new FileService(new FakeFileRepository(status), new FakeFileStorage(), new FakeObjectKeyGenerator());

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            service.GetDownloadPayloadAsync(1, false, Context(), CancellationToken.None));

        Assert.Equal(ApiCodes.NotFound, exception.Code);
    }

    [Fact]
    public async Task GetDownloadPayloadAsync_ReturnsNotFoundWhenMissing()
    {
        var service = new FileService(new MissingFileRepository(), new FakeFileStorage(), new FakeObjectKeyGenerator());

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            service.GetDownloadPayloadAsync(1, false, Context(), CancellationToken.None));

        Assert.Equal(ApiCodes.NotFound, exception.Code);
    }

    private sealed class FakeFileRepository : IFileRepository
    {
        private readonly string _status;

        public FakeFileRepository(string status = "active") => _status = status;

        public Task<PagedResult<FileSummaryDto>> ListAsync(FileListCriteria criteria, CancellationToken cancellationToken) => Task.FromResult(new PagedResult<FileSummaryDto>([], criteria.Page, criteria.PageSize, 0));
        public Task<FileDetailDto?> GetAsync(long id, CancellationToken cancellationToken) => Task.FromResult<FileDetailDto?>(new FileDetailDto(id, "a.pdf", ".pdf", "application/pdf", 100, "a".PadLeft(64, '0'), "active", 1, DateTimeOffset.UnixEpoch));
        public Task<FileDownloadRecord?> GetDownloadAsync(long id, CancellationToken cancellationToken) => Task.FromResult<FileDownloadRecord?>(new FileDownloadRecord("a", "a.pdf", "application/pdf", 5, _status));
        public Task<long> CreateAsync(FileCreateRecord record, CancellationToken cancellationToken) => Task.FromResult(1L);
        public Task SoftDeleteAsync(long id, DateTimeOffset now, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task RecordAuditAsync(FileAuditRecord record, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class RecordingFileRepository : IFileRepository
    {
        public FileCreateRecord? Recorded { get; private set; }

        public Task<PagedResult<FileSummaryDto>> ListAsync(FileListCriteria criteria, CancellationToken cancellationToken) => Task.FromResult(new PagedResult<FileSummaryDto>([], criteria.Page, criteria.PageSize, 0));
        public Task<FileDetailDto?> GetAsync(long id, CancellationToken cancellationToken) => Task.FromResult<FileDetailDto?>(null);
        public Task<FileDownloadRecord?> GetDownloadAsync(long id, CancellationToken cancellationToken) => Task.FromResult<FileDownloadRecord?>(null);
        public Task<long> CreateAsync(FileCreateRecord record, CancellationToken cancellationToken)
        {
            Recorded = record;
            return Task.FromResult(1L);
        }

        public Task SoftDeleteAsync(long id, DateTimeOffset now, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task RecordAuditAsync(FileAuditRecord record, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class MissingFileRepository : IFileRepository
    {
        public Task<PagedResult<FileSummaryDto>> ListAsync(FileListCriteria criteria, CancellationToken cancellationToken) => Task.FromResult(new PagedResult<FileSummaryDto>([], criteria.Page, criteria.PageSize, 0));
        public Task<FileDetailDto?> GetAsync(long id, CancellationToken cancellationToken) => Task.FromResult<FileDetailDto?>(null);
        public Task<FileDownloadRecord?> GetDownloadAsync(long id, CancellationToken cancellationToken) => Task.FromResult<FileDownloadRecord?>(null);
        public Task<long> CreateAsync(FileCreateRecord record, CancellationToken cancellationToken) => Task.FromResult(1L);
        public Task SoftDeleteAsync(long id, DateTimeOffset now, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task RecordAuditAsync(FileAuditRecord record, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeFileStorage : IFileStorage
    {
        public async Task<FileStorageResult> StoreAsync(Stream source, string objectKey, string fileExt, long maxSizeBytes, CancellationToken cancellationToken)
        {
            await using var memoryStream = new MemoryStream();
            await source.CopyToAsync(memoryStream, cancellationToken);
            if (memoryStream.Length > maxSizeBytes)
            {
                throw new DomainException(ApiCodes.ValidationError, "file size must not exceed max limit.");
            }

            var bytes = memoryStream.ToArray();
            using var sha = SHA256.Create();
            var hash = Convert.ToHexString(sha.ComputeHash(bytes)).ToLowerInvariant();
            var mimeType = fileExt switch
            {
                ".pdf" => "application/pdf",
                ".txt" => "text/plain",
                ".png" => "image/png",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };

            return new FileStorageResult(bytes.Length, hash, mimeType);
        }

        public Task DeleteAsync(string objectKey, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task<Stream> OpenReadAsync(string objectKey, CancellationToken cancellationToken)
        {
            return Task.FromResult<Stream>(new MemoryStream([0x25, 0x50, 0x44, 0x46, 0x2D]));
        }
    }

    private sealed class FakeObjectKeyGenerator : IFileObjectKeyGenerator
    {
        public string GenerateObjectKey(DateTimeOffset now, string fileExt)
        {
            return $"2026/06/test{fileExt}";
        }
    }

    private static string ComputeSha256(byte[] bytes)
    {
        using var sha = SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(bytes)).ToLowerInvariant();
    }
}
