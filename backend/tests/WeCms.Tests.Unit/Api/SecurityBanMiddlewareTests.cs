using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using WeCms.Api.Middleware;
using WeCms.Modules.System.Auth;
using WeCms.Modules.System.Security;
using WeCms.Shared;

namespace WeCms.Tests.Unit.Api;

public sealed class SecurityBanMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_IpBanBlocksRequestAndWritesSecurityEvent()
    {
        var nextCalled = false;
        var middleware = new SecurityBanMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = CreateContext("192.168.1.10");
        var service = new FakeSecurityBanService
        {
            IpBan = Ban(SecurityBanTypes.Ip, "192.168.1.10")
        };

        await middleware.InvokeAsync(context, service, new FakeAuthClock());

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        Assert.Equal(1, service.RecordHitCount);
    }

    [Fact]
    public async Task InvokeAsync_UserBanBlocksAuthenticatedRequest()
    {
        var middleware = new SecurityBanMiddleware(_ => Task.CompletedTask);
        var context = CreateContext("192.168.1.10", userId: 42);
        var service = new FakeSecurityBanService
        {
            UserBan = Ban(SecurityBanTypes.User, "42")
        };

        await middleware.InvokeAsync(context, service, new FakeAuthClock());

        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        Assert.Equal(1, service.RecordHitCount);
    }

    [Fact]
    public async Task InvokeAsync_NoBanCallsNext()
    {
        var nextCalled = false;
        var middleware = new SecurityBanMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = CreateContext("192.168.1.10", userId: 42);

        await middleware.InvokeAsync(context, new FakeSecurityBanService(), new FakeAuthClock());

        Assert.True(nextCalled);
        Assert.Equal(200, context.Response.StatusCode);
    }

    private static DefaultHttpContext CreateContext(string remoteIp, long? userId = null)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(remoteIp);
        context.TraceIdentifier = "trace-ban";
        context.Response.Body = new MemoryStream();

        if (userId is not null)
        {
            var identity = new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString(global::System.Globalization.CultureInfo.InvariantCulture)), new Claim(ClaimTypes.Name, "admin")],
                authenticationType: "test");
            context.User = new ClaimsPrincipal(identity);
        }

        return context;
    }

    private static SecurityBanRecord Ban(string banType, string target)
    {
        return new SecurityBanRecord(7, banType, target, "test", "warning", "unit", null, null);
    }

    private sealed class FakeAuthClock : IAuthClock
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 6, 18, 0, 0, 0, TimeSpan.Zero);
    }

    private sealed class FakeSecurityBanService : ISecurityBanService
    {
        public SecurityBanRecord? IpBan { get; init; }

        public SecurityBanRecord? UserBan { get; init; }

        public int RecordHitCount { get; private set; }

        public Task<SecurityStatusDto> GetStatusAsync(DateTimeOffset now, CancellationToken cancellationToken)
        {
            return Task.FromResult(new SecurityStatusDto(0, 0, 0, 0, now));
        }

        public Task<PagedResult<SecurityBanSummaryDto>> ListAsync(SecurityBanListQuery query, CancellationToken cancellationToken)
        {
            return Task.FromResult(new PagedResult<SecurityBanSummaryDto>([], query.Page, query.PageSize, 0));
        }

        public Task<SecurityBanDetailDto> GetAsync(long id, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<SecurityBanMutationResponse> UnbanAsync(long id, UnbanSecurityBanRequest request, SecurityBanRequestContext context, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<BatchUnbanSecurityBansResponse> BatchUnbanAsync(BatchUnbanSecurityBansRequest request, SecurityBanRequestContext context, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<SecurityBanMutationResponse> CreateTemporaryAsync(CreateSecurityBanRecord record, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<SecurityBanRecord?> FindActiveAsync(string banType, string target, DateTimeOffset now, CancellationToken cancellationToken)
        {
            return Task.FromResult(banType == SecurityBanTypes.Ip ? IpBan : UserBan);
        }

        public Task RecordHitAsync(SecurityBanRecord ban, SecurityBanHitContext context, CancellationToken cancellationToken)
        {
            RecordHitCount++;
            return Task.CompletedTask;
        }
    }
}
