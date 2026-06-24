using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace WeCms.Modules.FileCenter.Files;

public sealed class FileUploadTempCleanupService : IHostedService
{
    private readonly IOptions<FileUploadOptions> _fileUploadOptions;
    private readonly ILogger<FileUploadTempCleanupService> _logger;

    public FileUploadTempCleanupService(
        IOptions<FileUploadOptions> fileUploadOptions,
        ILogger<FileUploadTempCleanupService> logger)
    {
        _fileUploadOptions = fileUploadOptions;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            FileUploadContent.CleanupExpiredTempFiles(_fileUploadOptions.Value, _logger);
            _logger.LogDebug("Cleanup for expired file upload temp files completed.");
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Failed to cleanup expired upload temporary files.");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
