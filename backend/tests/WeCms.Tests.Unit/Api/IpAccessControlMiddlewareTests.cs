using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WeCms.Api.Middleware;
using WeCms.Api.Security;
using WeCms.Modules.Identity.Services;
using WeCms.Shared.Security;

namespace WeCms.Tests.Unit.Api;

public sealed class IpAccessControlMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_DisabledPolicyCallsNext()
    {
        var nextCalled = false;
        var buffer = new FakeSecurityRejectionEventBuffer();
        var logger = new CapturingLogger<IpAccessControlMiddleware>();
        var middleware = new IpAccessControlMiddleware(
            _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            new FakeOptionsMonitor(new IpAccessControlOptions()),
            new IpRuleMatcher(),
            buffer,
            new FakeAuthClock(),
            logger);
        var context = CreateContext("/api/v1/auth/login", "203.0.113.20");
        context.TraceIdentifier = "trace-ip-denied";

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
        Assert.Equal(200, context.Response.StatusCode);
        Assert.Empty(logger.Messages);
        Assert.Equal(0, buffer.Count);
    }

    [Fact]
    public async Task InvokeAsync_RejectsNonMatchingIpAndEnqueuesSecurityEvent()
    {
        var nextCalled = false;
        var buffer = new FakeSecurityRejectionEventBuffer();
        var logger = new CapturingLogger<IpAccessControlMiddleware>();
        var middleware = new IpAccessControlMiddleware(
            _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            new FakeOptionsMonitor(new IpAccessControlOptions
            {
                Enabled = true,
                AllowedRules = ["192.168.10.0/24"]
            }),
            new IpRuleMatcher(),
            buffer,
            new FakeAuthClock(),
            logger);

        var context = CreateContext("/api/v1/auth/login", "203.0.113.20");
        context.TraceIdentifier = "trace-ip-denied";

        await middleware.InvokeAsync(context);

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        Assert.Empty(logger.Messages);
        Assert.Equal(1, buffer.Count);
        Assert.Equal(SecurityRejectionEventKind.IpAccessDenied, buffer.LastRecord?.Kind);
        Assert.Equal("203.0.113.20", buffer.LastRecord?.IpAccessDenied?.Ip);
        Assert.Equal("trace-ip-denied", buffer.LastRecord?.IpAccessDenied?.TraceId);
    }

    [Fact]
    public async Task InvokeAsync_DoesNotThrowSecondaryExceptionWhenDenyResponseAlreadyStarted()
    {
        var buffer = new FakeSecurityRejectionEventBuffer();
        var logger = new CapturingLogger<IpAccessControlMiddleware>();
        var middleware = new IpAccessControlMiddleware(
            _ => Task.CompletedTask,
            new FakeOptionsMonitor(new IpAccessControlOptions
            {
                Enabled = true,
                AllowedRules = ["192.168.10.0/24"]
            }),
            new IpRuleMatcher(),
            buffer,
            new FakeAuthClock(),
            logger);

        var context = CreateContext("/api/v1/auth/login", "203.0.113.20");
        context.Features.Set<Microsoft.AspNetCore.Http.Features.IHttpResponseFeature>(new StartedResponseFeature());

        await middleware.InvokeAsync(context);

        Assert.True(context.Response.HasStarted);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Equal(1, buffer.Count);
        Assert.Empty(logger.Messages);
    }

    [Fact]
    public async Task InvokeAsync_AllowsMatchingIp()
    {
        var nextCalled = false;
        var middleware = new IpAccessControlMiddleware(
            _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            new FakeOptionsMonitor(new IpAccessControlOptions
            {
                Enabled = true,
                AllowedRules = ["192.168.10.0/24"]
            }),
            new IpRuleMatcher(),
            new FakeSecurityRejectionEventBuffer(),
            new FakeAuthClock(),
            new CapturingLogger<IpAccessControlMiddleware>());
        var context = CreateContext("/api/v1/system/users", "192.168.10.42");

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
        Assert.Equal(200, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_SkipsHealthEndpointWhenConfigured()
    {
        var nextCalled = false;
        var middleware = new IpAccessControlMiddleware(
            _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            new FakeOptionsMonitor(new IpAccessControlOptions
            {
                Enabled = true,
                SkipHealthEndpoints = true,
                AllowedRules = ["192.168.10.0/24"]
            }),
            new IpRuleMatcher(),
            new FakeSecurityRejectionEventBuffer(),
            new FakeAuthClock(),
            new CapturingLogger<IpAccessControlMiddleware>());
        var context = CreateContext("/health/live", "203.0.113.20");

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_RejectedIpLogsWarningWhenEventBufferDropOccurs()
    {
        var nextCalled = false;
        var logger = new CapturingLogger<IpAccessControlMiddleware>();
        var buffer = new FakeSecurityRejectionEventBuffer { TryEnqueueResult = false };
        var middleware = new IpAccessControlMiddleware(
            _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            new FakeOptionsMonitor(new IpAccessControlOptions
            {
                Enabled = true,
                AllowedRules = ["192.168.10.0/24"]
            }),
            new IpRuleMatcher(),
            buffer,
            new FakeAuthClock(),
            logger);
        var context = CreateContext("/api/v1/auth/login", "203.0.113.20");
        context.TraceIdentifier = "trace-ip-drop";

        await middleware.InvokeAsync(context);

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        Assert.Equal(1, buffer.Count);
        Assert.Contains(logger.Messages, message => message.Contains("Security rejection event was dropped due to full security rejection event buffer.", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Middleware_EnqueuesDeniedIpEventsWithoutSynchronousPersistence()
    {
        var source = await File.ReadAllTextAsync(
            RepoPath("backend", "src", "WeCms.Api", "Middleware", "IpAccessControlMiddleware.cs"),
            TestContext.Current.CancellationToken);

        Assert.Contains("ISecurityRejectionEventBuffer", source, StringComparison.Ordinal);
        Assert.Contains("TryEnqueue", source, StringComparison.Ordinal);
        Assert.DoesNotContain("RecordSecurityEventAsync", source, StringComparison.Ordinal);
        Assert.DoesNotContain("PublishIfRequiredAsync", source, StringComparison.Ordinal);
    }

    private static DefaultHttpContext CreateContext(string path, string remoteIp)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Connection.RemoteIpAddress = IPAddress.Parse(remoteIp);
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static string RepoPath(params string[] segments)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "backend", "WeCms.slnx")))
            {
                return Path.Combine([directory.FullName, .. segments]);
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }

    private sealed class FakeAuthClock : IAuthClock
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 6, 18, 0, 0, 0, TimeSpan.Zero);
    }

    private sealed class FakeSecurityRejectionEventBuffer : ISecurityRejectionEventBuffer
    {
        public int Count { get; private set; }
        public bool TryEnqueueResult { get; set; } = true;

        public SecurityRejectionEvent? LastRecord { get; private set; }

        public bool TryEnqueue(SecurityRejectionEvent record)
        {
            Count++;
            LastRecord = record;
            return TryEnqueueResult;
        }
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }
    }

    private sealed class FakeOptionsMonitor : IOptionsMonitor<IpAccessControlOptions>
    {
        public FakeOptionsMonitor(IpAccessControlOptions currentValue)
        {
            CurrentValue = currentValue;
        }

        public IpAccessControlOptions CurrentValue { get; }

        public IpAccessControlOptions Get(string? name) => CurrentValue;

        public IDisposable OnChange(Action<IpAccessControlOptions, string> listener) => new NoopDisposable();
    }

    private sealed class NoopDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
