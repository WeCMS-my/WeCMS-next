using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using WeCms.Api.Middleware;
using WeCms.Api.Security;
using WeCms.Modules.Identity.Services;
using WeCms.Modules.Security;
using WeCms.Shared.Security;

namespace WeCms.Tests.Unit.Api;

public sealed class IpAccessControlMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_DisabledPolicyCallsNext()
    {
        var nextCalled = false;
        var middleware = new IpAccessControlMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = CreateContext("/api/v1/auth/login", "203.0.113.20");
        context.TraceIdentifier = "trace-ip-denied";
        var buffer = new FakeSecurityRejectionEventBuffer();

        await middleware.InvokeAsync(
            context,
            Configuration(new Dictionary<string, string?>()),
            new IpRuleMatcher(),
            buffer,
            new FakeAuthClock());

        Assert.True(nextCalled);
        Assert.Equal(200, context.Response.StatusCode);
        Assert.Equal(0, buffer.Count);
    }

    [Fact]
    public async Task InvokeAsync_RejectsNonMatchingIpAndEnqueuesSecurityEvent()
    {
        var nextCalled = false;
        var middleware = new IpAccessControlMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = CreateContext("/api/v1/auth/login", "203.0.113.20");
        context.TraceIdentifier = "trace-ip-denied";
        var buffer = new FakeSecurityRejectionEventBuffer();

        await middleware.InvokeAsync(
            context,
            Configuration(new Dictionary<string, string?>
            {
                ["Security:IpAccessControl:Enabled"] = "true",
                ["Security:IpAccessControl:AllowedRules:0"] = "192.168.10.0/24"
            }),
            new IpRuleMatcher(),
            buffer,
            new FakeAuthClock());

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        Assert.Equal(1, buffer.Count);
        Assert.Equal(SecurityRejectionEventKind.IpAccessDenied, buffer.LastRecord?.Kind);
        Assert.Equal("203.0.113.20", buffer.LastRecord?.IpAccessDenied?.Ip);
        Assert.Equal("trace-ip-denied", buffer.LastRecord?.IpAccessDenied?.TraceId);
    }

    [Fact]
    public async Task InvokeAsync_DoesNotThrowSecondaryExceptionWhenDenyResponseAlreadyStarted()
    {
        var middleware = new IpAccessControlMiddleware(_ => Task.CompletedTask);
        var context = CreateContext("/api/v1/auth/login", "203.0.113.20");
        context.Features.Set<Microsoft.AspNetCore.Http.Features.IHttpResponseFeature>(new StartedResponseFeature());
        var buffer = new FakeSecurityRejectionEventBuffer();

        await middleware.InvokeAsync(
            context,
            Configuration(new Dictionary<string, string?>
            {
                ["Security:IpAccessControl:Enabled"] = "true",
                ["Security:IpAccessControl:AllowedRules:0"] = "192.168.10.0/24"
            }),
            new IpRuleMatcher(),
            buffer,
            new FakeAuthClock());

        Assert.True(context.Response.HasStarted);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Equal(1, buffer.Count);
    }

    [Fact]
    public async Task InvokeAsync_AllowsMatchingIp()
    {
        var nextCalled = false;
        var middleware = new IpAccessControlMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = CreateContext("/api/v1/system/users", "192.168.10.42");

        await middleware.InvokeAsync(
            context,
            Configuration(new Dictionary<string, string?>
            {
                ["Security:IpAccessControl:Enabled"] = "true",
                ["Security:IpAccessControl:AllowedRules:0"] = "192.168.10.0/24"
            }),
            new IpRuleMatcher(),
            new FakeSecurityRejectionEventBuffer(),
            new FakeAuthClock());

        Assert.True(nextCalled);
        Assert.Equal(200, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_SkipsHealthEndpointWhenConfigured()
    {
        var nextCalled = false;
        var middleware = new IpAccessControlMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = CreateContext("/health/live", "203.0.113.20");

        await middleware.InvokeAsync(
            context,
            Configuration(new Dictionary<string, string?>
            {
                ["Security:IpAccessControl:Enabled"] = "true",
                ["Security:IpAccessControl:SkipHealthEndpoints"] = "true",
                ["Security:IpAccessControl:AllowedRules:0"] = "192.168.10.0/24"
            }),
            new IpRuleMatcher(),
            new FakeSecurityRejectionEventBuffer(),
            new FakeAuthClock());

        Assert.True(nextCalled);
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

    private static IConfiguration Configuration(IReadOnlyDictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
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

        public SecurityRejectionEvent? LastRecord { get; private set; }

        public bool TryEnqueue(SecurityRejectionEvent record)
        {
            Count++;
            LastRecord = record;
            return true;
        }
    }
}
