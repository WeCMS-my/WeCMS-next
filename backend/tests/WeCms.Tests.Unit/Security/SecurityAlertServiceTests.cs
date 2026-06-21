using WeCms.Modules.Security;

namespace WeCms.Tests.Unit.Security;

public sealed class SecurityAlertServiceTests
{
    [Fact]
    public async Task PublishIfRequiredAsync_CriticalEvent_CallsSink()
    {
        var sink = new FakeSecurityAlertSink();
        var service = new SecurityAlertService(sink);

        await service.PublishIfRequiredAsync(
            new SecurityAlertRecord(
                "refresh_token_reuse",
                "critical",
                "auth",
                "Refresh token reuse detected.",
                "trace-alert",
                DateTimeOffset.Parse("2026-06-19T00:00:00Z", global::System.Globalization.CultureInfo.InvariantCulture)),
            CancellationToken.None);

        var record = Assert.Single(sink.Records);
        Assert.Equal("refresh_token_reuse", record.EventType);
    }

    [Theory]
    [InlineData("info")]
    [InlineData("warning")]
    public async Task PublishIfRequiredAsync_NonCriticalEvent_DoesNotCallSink(string severity)
    {
        var sink = new FakeSecurityAlertSink();
        var service = new SecurityAlertService(sink);

        await service.PublishIfRequiredAsync(
            new SecurityAlertRecord(
                "permission_denied",
                severity,
                "permission",
                "Permission denied.",
                "trace-alert",
                DateTimeOffset.UtcNow),
            CancellationToken.None);

        Assert.Empty(sink.Records);
    }

    private sealed class FakeSecurityAlertSink : ISecurityAlertSink
    {
        public List<SecurityAlertRecord> Records { get; } = [];

        public Task SendAsync(SecurityAlertRecord record, CancellationToken cancellationToken)
        {
            Records.Add(record);
            return Task.CompletedTask;
        }
    }
}
