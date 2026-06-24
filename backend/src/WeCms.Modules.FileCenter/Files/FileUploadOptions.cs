namespace WeCms.Modules.FileCenter.Files;

public sealed class FileUploadOptions
{
    public const string SectionName = "FileStorage:Upload";

    public const int DefaultChunkSizeBytes = 8192;
    public const long DefaultMemoryFallbackThresholdBytes = 4L * 1024 * 1024;
    public const int DefaultRetryCount = 1;
    public const int DefaultRetryDelayMilliseconds = 1;
    public const int DefaultTempFileRetentionHours = 24;
    public const int MaxTempFileRetentionHours = 720;
    public const int DefaultMaxConcurrentLargeFileUploads = 4;
    public const int MaxConcurrentLargeFileUploadsLimit = 128;

    public int ChunkSizeBytes { get; init; } = DefaultChunkSizeBytes;
    public long MemoryFallbackThresholdBytes { get; init; } = DefaultMemoryFallbackThresholdBytes;
    public string TempFilePath { get; init; } = string.Empty;
    public int RetryCount { get; init; } = DefaultRetryCount;
    public int RetryDelayMilliseconds { get; init; } = DefaultRetryDelayMilliseconds;
    public int TempFileRetentionHours { get; init; } = DefaultTempFileRetentionHours;
    public int MaxConcurrentLargeFileUploads { get; init; } = DefaultMaxConcurrentLargeFileUploads;

    public static FileUploadOptions Default { get; } = new();
}
