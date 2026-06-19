using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.AspNetCore.Http;
using WeCms.Modules.System.Files;
using WeCms.Shared;

namespace WeCms.Tests.Unit.Files;

public sealed class FileServiceTests
{
    [Fact]
    public void AddWeCmsSystemFiles_DefaultsToNoopScannerAndResolvesFileService()
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IFileRepository, FakeFileRepository>()
            .AddSingleton<IFileStorage, FakeFileStorage>()
            .AddWeCmsSystemFiles();

        using var provider = services.BuildServiceProvider();
        var scanner = provider.GetRequiredService<IFileScanService>();

        Assert.IsType<NoopFileScanService>(scanner);
        var service = provider.GetRequiredService<IFileService>();

        Assert.IsType<FileService>(service);
    }

    [Fact]
    public async Task CreateAsync_RejectsFileLargerThanTenMiB()
    {
        var service = CreateService();
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
        var repository = new FakeFileRepository();
        var service = CreateService(repository);
        var file = CreateFormFile("a.pdf", "application/pdf", [0x25, 0x50, 0x44, 0x46, 0x2D]);

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            service.CreateAsync(
                new CreateFileRequest("a.exe", "application/octet-stream", file.Length, "a".PadLeft(64, '0')),
                file,
                Context(),
                CancellationToken.None));

        Assert.Equal(ApiCodes.ValidationError, exception.Code);
        Assert.Equal("file_upload_rejected", repository.LastSecurityEvent?.EventType);
        Assert.Equal("warning", repository.LastSecurityEvent?.Severity);
    }

    [Fact]
    public async Task CreateAsync_RejectsDisallowedExtension()
    {
        var service = CreateService();
        var file = CreateFormFile("a.exe", "text/plain", [0x41, 0x42, 0x43]);

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            service.CreateAsync(
                new CreateFileRequest("a.exe", "application/pdf", file.Length, "a".PadLeft(64, '0')),
                file,
                Context(),
                CancellationToken.None));

        Assert.Equal(ApiCodes.ValidationError, exception.Code);
    }

    [Theory]
    [InlineData("bad\rname.pdf")]
    [InlineData("bad\nname.pdf")]
    [InlineData("bad\"name.pdf")]
    [InlineData("bad\\name.pdf")]
    [InlineData("bad;name.pdf")]
    public async Task CreateAsync_RejectsHeaderDangerousOriginalName(string originalName)
    {
        var service = CreateService();
        var file = CreateFormFile("safe.pdf", "application/pdf", [0x25, 0x50, 0x44, 0x46, 0x2D]);

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            service.CreateAsync(
                new CreateFileRequest(originalName, "application/pdf", file.Length, "a".PadLeft(64, '0')),
                file,
                Context(),
                CancellationToken.None));

        Assert.Equal(ApiCodes.ValidationError, exception.Code);
        Assert.Equal("originalName contains invalid characters.", exception.Message);
    }

    [Fact]
    public async Task CreateAsync_RejectsSha256Mismatch()
    {
        var service = CreateService();
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
        var service = CreateService(repository);
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
        Assert.StartsWith("documents/", repository.Recorded!.ObjectKey, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateAsync_RejectsAvatarPolicyForSystemFileUpload()
    {
        var service = CreateService();
        var content = Encoding.UTF8.GetBytes("sample content");
        var file = CreateFormFile("upload.pdf", "application/pdf", content);

        var exception = await Assert.ThrowsAsync<DomainException>(() => service.CreateAsync(
            new CreateFileRequest("upload.pdf", "application/pdf", content.Length, ComputeSha256(content), "avatar"),
            file,
            Context(),
            CancellationToken.None));

        Assert.Equal(ApiCodes.ValidationError, exception.Code);
    }

    [Fact]
    public async Task CreateAsync_RejectsImagePolicyWhenImageStructureIsInvalid()
    {
        var service = CreateService();
        var content = Encoding.UTF8.GetBytes("not a real png");
        var file = CreateFormFile("upload.png", "image/png", content);

        var exception = await Assert.ThrowsAsync<DomainException>(() => service.CreateAsync(
            new CreateFileRequest("upload.png", "image/png", content.Length, ComputeSha256(content), "image"),
            file,
            Context(),
            CancellationToken.None));

        Assert.Equal(ApiCodes.ValidationError, exception.Code);
    }

    [Fact]
    public async Task CreateAsync_RejectsWhenFileScannerRejectsContentAndWritesSecurityEvent()
    {
        var repository = new FakeFileRepository();
        var service = CreateService(repository, scanner: new RejectingFileScanService());
        var content = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D };
        var file = CreateFormFile("upload.pdf", "application/pdf", content);

        var exception = await Assert.ThrowsAsync<DomainException>(() => service.CreateAsync(
            new CreateFileRequest("upload.pdf", "application/pdf", content.Length, ComputeSha256(content)),
            file,
            Context(),
            CancellationToken.None));

        Assert.Equal(ApiCodes.ValidationError, exception.Code);
        Assert.Equal("file_upload_rejected", repository.LastSecurityEvent?.EventType);
    }


    [Fact]
    public async Task CreateAsync_CleansUpUploadedFileAndLogsWarning_WhenCleanupFails()
    {
        var logger = new RecordingLogger();
        var storage = new FailingFileStorage();
        var service = CreateService(storage: storage, logger: logger);

        var content = Encoding.UTF8.GetBytes("upload content");
        var file = CreateFormFile("upload.pdf", "application/pdf", content);

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            service.CreateAsync(
                new CreateFileRequest("upload.pdf", "application/pdf", content.Length, ComputeSha256(content)),
                file,
                Context(),
                CancellationToken.None));

        Assert.Equal(ApiCodes.ValidationError, exception.Code);
        Assert.Single(logger.WarningMessages);
        Assert.Contains("Failed to cleanup file after upload failure", logger.WarningMessages[0], StringComparison.Ordinal);
        Assert.True(storage.DeleteAttempted);
        Assert.NotNull(storage.ThrownException);
    }

    [Fact]
    public async Task GetAsync_DoesNotExposeObjectKey()
    {
        var service = CreateService();

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
        var service = CreateService(new FakeFileRepository("active"));

        var payload = await service.GetDownloadPayloadAsync(1, false, Context(), CancellationToken.None);

        Assert.Equal("a.pdf", payload.FileName);
        Assert.Equal("application/pdf", payload.ContentType);
        Assert.Equal(5, payload.SizeBytes);
        Assert.False(payload.Inline);
        using var memory = new MemoryStream();
        await payload.Content.CopyToAsync(memory, CancellationToken.None);
        Assert.Equal(5, memory.Length);
    }

    [Fact]
    public async Task GetDownloadPayloadAsync_ForcesDocumentPreviewToAttachment()
    {
        var service = CreateService(new FakeFileRepository("active"));

        var payload = await service.GetDownloadPayloadAsync(1, true, Context(), CancellationToken.None);

        Assert.False(payload.Inline);
    }

    [Theory]
    [InlineData("disabled")]
    [InlineData("deleted")]
    public async Task GetDownloadPayloadAsync_ReturnsNotFoundWhenStatusIsNotActive(string status)
    {
        var service = CreateService(new FakeFileRepository(status));

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            service.GetDownloadPayloadAsync(1, false, Context(), CancellationToken.None));

        Assert.Equal(ApiCodes.NotFound, exception.Code);
    }

    [Fact]
    public async Task GetDownloadPayloadAsync_ReturnsNotFoundWhenMissing()
    {
        var service = CreateService(new MissingFileRepository());

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
        public Task<FileDownloadRecord?> GetDownloadAsync(long id, CancellationToken cancellationToken) => Task.FromResult<FileDownloadRecord?>(new FileDownloadRecord("a", "a.pdf", ".pdf", "application/pdf", 5, _status));
        public Task<long> CreateAsync(FileCreateRecord record, CancellationToken cancellationToken) => Task.FromResult(1L);
        public Task SoftDeleteAsync(long id, DateTimeOffset now, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task RecordAuditAsync(FileAuditRecord record, CancellationToken cancellationToken) => Task.CompletedTask;
        public FileSecurityEventRecord? LastSecurityEvent { get; private set; }
        public Task RecordSecurityEventAsync(FileSecurityEventRecord record, CancellationToken cancellationToken)
        {
            LastSecurityEvent = record;
            return Task.CompletedTask;
        }
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
        public Task RecordSecurityEventAsync(FileSecurityEventRecord record, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class MissingFileRepository : IFileRepository
    {
        public Task<PagedResult<FileSummaryDto>> ListAsync(FileListCriteria criteria, CancellationToken cancellationToken) => Task.FromResult(new PagedResult<FileSummaryDto>([], criteria.Page, criteria.PageSize, 0));
        public Task<FileDetailDto?> GetAsync(long id, CancellationToken cancellationToken) => Task.FromResult<FileDetailDto?>(null);
        public Task<FileDownloadRecord?> GetDownloadAsync(long id, CancellationToken cancellationToken) => Task.FromResult<FileDownloadRecord?>(null);
        public Task<long> CreateAsync(FileCreateRecord record, CancellationToken cancellationToken) => Task.FromResult(1L);
        public Task SoftDeleteAsync(long id, DateTimeOffset now, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task RecordAuditAsync(FileAuditRecord record, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task RecordSecurityEventAsync(FileSecurityEventRecord record, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FailingFileStorage : IFileStorage
    {
        public bool DeleteAttempted { get; private set; }
        public Exception? ThrownException { get; private set; }

        public Task<FileStorageResult> StoreAsync(Stream source, string objectKey, string fileExt, long maxSizeBytes, CancellationToken cancellationToken)
        {
            var bytes = new byte[source.Length == 0 ? 1 : source.Length];
            return Task.FromResult(new FileStorageResult(bytes.Length + 1, "0000", fileExt switch
            {
                ".pdf" => "application/pdf",
                ".txt" => "text/plain",
                ".png" => "image/png",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            }));
        }

        public Task DeleteAsync(string objectKey, CancellationToken cancellationToken)
        {
            DeleteAttempted = true;
            var exception = new IOException("delete unavailable");
            ThrownException = exception;
            return Task.FromException(exception);
        }

        public Task<Stream> OpenReadAsync(string objectKey, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<bool> ExistsAsync(string objectKey, CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }

        public Task<FileStorageMetadata> GetMetadataAsync(string objectKey, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class RecordingLogger : ILogger<FileService>
    {
        public List<string> WarningMessages { get; } = new();

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning)
            {
                WarningMessages.Add(formatter(state, exception));
            }
        }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            return NullScope.Instance;
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose()
            {
            }
        }
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

        public Task<bool> ExistsAsync(string objectKey, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public Task<FileStorageMetadata> GetMetadataAsync(string objectKey, CancellationToken cancellationToken)
        {
            return Task.FromResult(new FileStorageMetadata(5, "application/pdf", DateTimeOffset.UnixEpoch));
        }
    }

    private sealed class CleanFileScanService : IFileScanService
    {
        public Task<FileScanResult> ScanAsync(Stream source, FileScanRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(FileScanResult.CleanResult);
        }
    }

    private sealed class RejectingFileScanService : IFileScanService
    {
        public Task<FileScanResult> ScanAsync(Stream source, FileScanRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new FileScanResult(false, "unit-test-rejection"));
        }
    }

    private sealed class FakeObjectKeyGenerator : IFileObjectKeyGenerator
    {
        public string GenerateObjectKey(DateTimeOffset now, string fileExt)
        {
            return $"2026/06/test{fileExt}";
        }
    }

    private static FileService CreateService(IFileRepository? repository = null, IFileStorage? storage = null, IFileScanService? scanner = null, RecordingLogger? logger = null)
    {
        return new FileService(
            repository ?? new FakeFileRepository(),
            storage ?? new FakeFileStorage(),
            scanner ?? new CleanFileScanService(),
            new FakeObjectKeyGenerator(),
            new FileUploadPolicyResolver([new AvatarUploadPolicy(), new ImageUploadPolicy(), new DocumentUploadPolicy()]),
            logger ?? new RecordingLogger());
    }

    private static string ComputeSha256(byte[] bytes)
    {
        using var sha = SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(bytes)).ToLowerInvariant();
    }
}
