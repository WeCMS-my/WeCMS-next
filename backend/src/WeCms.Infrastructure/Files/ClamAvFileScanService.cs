using System.Buffers.Binary;
using System.Net.Sockets;
using System.Text;
using WeCms.Shared;

namespace WeCms.Infrastructure.Files;

public sealed class ClamAvFileScanOptions
{
    public const int DefaultPort = 3310;
    public const int DefaultTimeoutSeconds = 10;
    public const int DefaultChunkSizeBytes = 8192;

    public ClamAvFileScanOptions(string host, int port, int timeoutSeconds)
        : this(host, port, timeoutSeconds, DefaultChunkSizeBytes)
    {
    }

    public ClamAvFileScanOptions(string host, int port, int timeoutSeconds, int chunkSizeBytes)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            throw new ArgumentException("ClamAV host is required.", nameof(host));
        }

        if (port is < 1 or > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(port), "ClamAV port must be between 1 and 65535.");
        }

        if (timeoutSeconds is < 1 or > 300)
        {
            throw new ArgumentOutOfRangeException(nameof(timeoutSeconds), "ClamAV timeout must be between 1 and 300 seconds.");
        }

        if (chunkSizeBytes is < 1024 or > 1024 * 1024)
        {
            throw new ArgumentOutOfRangeException(nameof(chunkSizeBytes), "ClamAV chunk size must be between 1024 bytes and 1 MiB.");
        }

        Host = host.Trim();
        Port = port;
        TimeoutSeconds = timeoutSeconds;
        ChunkSizeBytes = chunkSizeBytes;
    }

    public string Host { get; }

    public int Port { get; }

    public int TimeoutSeconds { get; }

    public int ChunkSizeBytes { get; }
}

public sealed class ClamAvFileScanService : IFileScanService
{
    private const string InStreamCommand = "zINSTREAM\0";
    private readonly ClamAvFileScanOptions _options;

    public ClamAvFileScanService(ClamAvFileScanOptions options)
    {
        _options = options;
    }

    public async Task<FileScanResult> ScanAsync(Stream source, FileScanRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(request);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));

        using var client = new TcpClient();
        await client.ConnectAsync(_options.Host, _options.Port, timeoutCts.Token);
        await using var network = client.GetStream();

        await network.WriteAsync(Encoding.ASCII.GetBytes(InStreamCommand), timeoutCts.Token);
        await WriteStreamChunksAsync(source, network, timeoutCts.Token);
        var response = await ReadResponseAsync(network, timeoutCts.Token);

        return ToScanResult(response);
    }

    private async Task WriteStreamChunksAsync(Stream source, NetworkStream network, CancellationToken cancellationToken)
    {
        var buffer = new byte[_options.ChunkSizeBytes];
        var lengthPrefix = new byte[4];
        int read;
        while ((read = await source.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
        {
            BinaryPrimitives.WriteInt32BigEndian(lengthPrefix, read);
            await network.WriteAsync(lengthPrefix, cancellationToken);
            await network.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }

        BinaryPrimitives.WriteInt32BigEndian(lengthPrefix, 0);
        await network.WriteAsync(lengthPrefix, cancellationToken);
    }

    private static async Task<string> ReadResponseAsync(NetworkStream network, CancellationToken cancellationToken)
    {
        using var response = new MemoryStream();
        var buffer = new byte[512];
        int read;
        while ((read = await network.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await response.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            if (buffer.AsSpan(0, read).Contains((byte)'\0') || buffer.AsSpan(0, read).Contains((byte)'\n'))
            {
                break;
            }
        }

        return Encoding.UTF8.GetString(response.ToArray()).Trim('\0', '\r', '\n', ' ');
    }

    private static FileScanResult ToScanResult(string response)
    {
        if (response.Contains(" FOUND", StringComparison.OrdinalIgnoreCase))
        {
            return new FileScanResult(false, response);
        }

        if (response.EndsWith(" OK", StringComparison.OrdinalIgnoreCase) || string.Equals(response, "OK", StringComparison.OrdinalIgnoreCase))
        {
            return FileScanResult.CleanResult;
        }

        throw new InvalidOperationException($"Unexpected ClamAV scan response: {response}");
    }
}
