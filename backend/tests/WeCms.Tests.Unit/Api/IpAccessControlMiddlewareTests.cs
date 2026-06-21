using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using WeCms.Api.Middleware;
using WeCms.Modules.Identity.Services;
using WeCms.Modules.AccessControl.Menus;
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
        var repository = new FakeAuthRepository();

        await middleware.InvokeAsync(
            context,
            Configuration(new Dictionary<string, string?>()),
            new IpRuleMatcher(),
            repository,
            new FakeSecurityAlertService(),
            new FakeAuthClock());

        Assert.True(nextCalled);
        Assert.Equal(200, context.Response.StatusCode);
        Assert.Equal(0, repository.SecurityEventCount);
    }

    [Fact]
    public async Task InvokeAsync_RejectsNonMatchingIpAndWritesSecurityEvent()
    {
        var nextCalled = false;
        var middleware = new IpAccessControlMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = CreateContext("/api/v1/auth/login", "203.0.113.20");
        context.TraceIdentifier = "trace-ip-denied";
        var repository = new FakeAuthRepository();
        var alertService = new FakeSecurityAlertService();

        await middleware.InvokeAsync(
            context,
            Configuration(new Dictionary<string, string?>
            {
                ["Security:IpAccessControl:Enabled"] = "true",
                ["Security:IpAccessControl:AllowedRules:0"] = "192.168.10.0/24"
            }),
            new IpRuleMatcher(),
            repository,
            alertService,
            new FakeAuthClock());

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        Assert.Equal(1, repository.SecurityEventCount);
        Assert.Equal("security.ip_rejected", repository.LastSecurityEventType);
        Assert.Equal("critical", repository.LastSecurityEventSeverity);
        Assert.Equal("trace-ip-denied", repository.LastTraceId);
        Assert.Equal(1, alertService.Count);
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
            new FakeAuthRepository(),
            new FakeSecurityAlertService(),
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
            new FakeAuthRepository(),
            new FakeSecurityAlertService(),
            new FakeAuthClock());

        Assert.True(nextCalled);
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

    private sealed class FakeAuthClock : IAuthClock
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 6, 18, 0, 0, 0, TimeSpan.Zero);
    }

    private sealed class FakeAuthRepository : IAuthRepository
    {
        public int SecurityEventCount { get; private set; }

        public string LastSecurityEventType { get; private set; } = string.Empty;

        public string LastSecurityEventSeverity { get; private set; } = string.Empty;

        public string LastTraceId { get; private set; } = string.Empty;

        public Task<AuthUserRecord?> FindUserByUsernameAsync(string username, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<AuthUserRecord?> FindUserByIdAsync(long userId, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<RefreshTokenRecord?> FindRefreshTokenByHashAsync(string tokenHash, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<string>> ListRoleCodesAsync(long userId, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<string>> ListPermissionCodesAsync(long userId, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<MenuSummaryDto>> ListVisibleMenusAsync(long userId, bool isSuperAdmin, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task RecordFailedLoginAsync(FailedLoginRecord record, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task RecordSecurityEventAsync(SecurityEventRecord record, CancellationToken cancellationToken)
        {
            SecurityEventCount++;
            LastSecurityEventType = record.EventType;
            LastSecurityEventSeverity = record.Severity;
            LastTraceId = record.TraceId ?? string.Empty;
            return Task.CompletedTask;
        }

        public Task RecordAuditLogAsync(AuditLogRecord record, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task CompleteSuccessfulLoginAsync(SuccessfulLoginRecord record, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task CompleteRefreshRotationAsync(RefreshRotationRecord record, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task RevokeRefreshTokenFamilyAsync(string familyId, DateTimeOffset revokedAt, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class FakeSecurityAlertService : ISecurityAlertService
    {
        public int Count { get; private set; }

        public Task PublishIfRequiredAsync(SecurityAlertRecord record, CancellationToken cancellationToken)
        {
            Count++;
            return Task.CompletedTask;
        }
    }
}
