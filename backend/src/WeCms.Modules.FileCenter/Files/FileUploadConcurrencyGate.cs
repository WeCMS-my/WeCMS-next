using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using WeCms.Shared;

namespace WeCms.Modules.FileCenter.Files;

public interface IFileUploadConcurrencyGate
{
    bool TryAcquire(long sizeBytes, out IFileUploadConcurrencyLease lease);

    FileUploadConcurrencyMetricsDto GetMetrics();
}

public interface IFileUploadConcurrencyLease : IAsyncDisposable
{
}

public sealed record FileUploadConcurrencyMetricsDto(
    [property: JsonPropertyName("file_upload_large_file_concurrency_limit")] int FileUploadLargeFileConcurrencyLimit,
    [property: JsonPropertyName("file_upload_large_file_active")] long FileUploadLargeFileActive,
    [property: JsonPropertyName("file_upload_large_file_rejected_total")] long FileUploadLargeFileRejectedTotal,
    [property: JsonPropertyName("file_upload_large_file_threshold_bytes")] long FileUploadLargeFileThresholdBytes);

public sealed class FileUploadConcurrencyGate : IFileUploadConcurrencyGate
{
    private readonly FileUploadOptions _options;
    private readonly SemaphoreSlim _largeFileSemaphore;
    private long _activeLargeFileUploads;
    private long _rejectedLargeFileUploads;

    public FileUploadConcurrencyGate(IOptions<FileUploadOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
        _largeFileSemaphore = new SemaphoreSlim(Math.Max(1, _options.MaxConcurrentLargeFileUploads));
    }

    public bool TryAcquire(long sizeBytes, out IFileUploadConcurrencyLease lease)
    {
        if (sizeBytes <= _options.MemoryFallbackThresholdBytes)
        {
            lease = NoopFileUploadConcurrencyLease.Instance;
            return true;
        }

        if (!_largeFileSemaphore.Wait(0))
        {
            Interlocked.Increment(ref _rejectedLargeFileUploads);
            lease = NoopFileUploadConcurrencyLease.Instance;
            return false;
        }

        Interlocked.Increment(ref _activeLargeFileUploads);
        lease = new LargeFileUploadConcurrencyLease(this);
        return true;
    }

    public FileUploadConcurrencyMetricsDto GetMetrics()
    {
        return new FileUploadConcurrencyMetricsDto(
            Math.Max(1, _options.MaxConcurrentLargeFileUploads),
            Interlocked.Read(ref _activeLargeFileUploads),
            Interlocked.Read(ref _rejectedLargeFileUploads),
            _options.MemoryFallbackThresholdBytes);
    }

    private void ReleaseLargeFile()
    {
        Interlocked.Decrement(ref _activeLargeFileUploads);
        _largeFileSemaphore.Release();
    }

    private sealed class LargeFileUploadConcurrencyLease : IFileUploadConcurrencyLease
    {
        private FileUploadConcurrencyGate? _owner;

        public LargeFileUploadConcurrencyLease(FileUploadConcurrencyGate owner)
        {
            _owner = owner;
        }

        public ValueTask DisposeAsync()
        {
            Interlocked.Exchange(ref _owner, null)?.ReleaseLargeFile();
            return ValueTask.CompletedTask;
        }
    }

    private sealed class NoopFileUploadConcurrencyLease : IFileUploadConcurrencyLease
    {
        public static readonly NoopFileUploadConcurrencyLease Instance = new();

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }
}
