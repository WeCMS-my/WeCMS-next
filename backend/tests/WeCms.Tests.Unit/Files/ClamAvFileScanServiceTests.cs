using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Text;
using WeCms.Infrastructure.Files;
using WeCms.Shared;

namespace WeCms.Tests.Unit.Files;

public sealed class ClamAvFileScanServiceTests
{
    [Fact]
    public async Task ScanAsync_ReturnsClean_WhenClamAvReportsOk()
    {
        await using var server = await FakeClamAvServer.StartAsync("stream: OK\0");
        var service = new ClamAvFileScanService(new ClamAvFileScanOptions("127.0.0.1", server.Port, 2, 1024));

        var result = await service.ScanAsync(
            new MemoryStream(Encoding.UTF8.GetBytes("safe file")),
            new FileScanRequest("safe.txt", "text/plain", 9, "document"),
            CancellationToken.None);

        Assert.True(result.Clean);
        Assert.Null(result.Reason);
        Assert.Equal("safe file", Encoding.UTF8.GetString(await server.ReceivedContentAsync()));
    }

    [Fact]
    public async Task ScanAsync_ReturnsRejected_WhenClamAvReportsFound()
    {
        await using var server = await FakeClamAvServer.StartAsync("stream: Eicar-Test-Signature FOUND\0");
        var service = new ClamAvFileScanService(new ClamAvFileScanOptions("127.0.0.1", server.Port, 2, 1024));

        var result = await service.ScanAsync(
            new MemoryStream(Encoding.UTF8.GetBytes("infected")),
            new FileScanRequest("eicar.txt", "text/plain", 8, "document"),
            CancellationToken.None);

        Assert.False(result.Clean);
        Assert.Contains("Eicar-Test-Signature FOUND", result.Reason, StringComparison.Ordinal);
    }

    private sealed class FakeClamAvServer : IAsyncDisposable
    {
        private readonly TcpListener _listener;
        private readonly Task<byte[]> _receivedContentTask;

        private FakeClamAvServer(TcpListener listener, string response)
        {
            _listener = listener;
            Port = ((IPEndPoint)listener.LocalEndpoint).Port;
            _receivedContentTask = AcceptOnceAsync(response);
        }

        public int Port { get; }

        public static Task<FakeClamAvServer> StartAsync(string response)
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            return Task.FromResult(new FakeClamAvServer(listener, response));
        }

        public async Task<byte[]> ReceivedContentAsync()
        {
            return await _receivedContentTask.WaitAsync(TimeSpan.FromSeconds(5));
        }

        public ValueTask DisposeAsync()
        {
            _listener.Stop();
            return ValueTask.CompletedTask;
        }

        private async Task<byte[]> AcceptOnceAsync(string response)
        {
            using var client = await _listener.AcceptTcpClientAsync();
            await using var stream = client.GetStream();

            var command = new byte["zINSTREAM\0".Length];
            await stream.ReadExactlyAsync(command);
            Assert.Equal("zINSTREAM\0", Encoding.ASCII.GetString(command));

            using var content = new MemoryStream();
            var lengthBuffer = new byte[4];
            while (true)
            {
                await stream.ReadExactlyAsync(lengthBuffer);
                var length = BinaryPrimitives.ReadInt32BigEndian(lengthBuffer);
                if (length == 0)
                {
                    break;
                }

                var buffer = new byte[length];
                await stream.ReadExactlyAsync(buffer);
                await content.WriteAsync(buffer);
            }

            var responseBytes = Encoding.ASCII.GetBytes(response);
            await stream.WriteAsync(responseBytes);
            return content.ToArray();
        }
    }
}
