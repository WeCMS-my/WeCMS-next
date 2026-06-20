using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using WeCms.Modules.System.Auth;
using WeCms.Modules.System.Permissions;
using WeCms.Modules.System.Files;
using WeCms.Shared;

namespace WeCms.Tests.Unit.Permissions;

public sealed class PermissionEndpointFilterTests
{
    [Fact]
    public async Task InvokeAsync_ReturnsUnauthorizedWhenUserIsMissing()
    {
        var (httpContext, filterContext) = CreateContext(PermissionCheckResult.Allowed, userId: null);
        var filter = CreateFilter(httpContext);

        var result = await filter.InvokeAsync(filterContext, _ => throw new InvalidOperationException("Next must not run."));

        var response = await ExecuteResultAsync(result, httpContext);
        Assert.Equal(ApiCodes.ToHttpStatus(ApiCodes.Unauthorized), response.StatusCode);
        Assert.Equal("Authentication is required.", response.Message);
    }

    [Fact]
    public async Task InvokeAsync_ReturnsUnauthorizedWhenUserIsDisabled()
    {
        var (httpContext, filterContext) = CreateContext(PermissionCheckResult.UserDisabled, userId: 42);
        var filter = CreateFilter(httpContext);

        var result = await filter.InvokeAsync(filterContext, _ => throw new InvalidOperationException("Next must not run."));

        var response = await ExecuteResultAsync(result, httpContext);
        Assert.Equal(ApiCodes.ToHttpStatus(ApiCodes.Unauthorized), response.StatusCode);
        Assert.Equal("User account is disabled.", response.Message);
        var writer = Assert.IsType<FakePermissionSecurityEventWriter>(
            httpContext.RequestServices.GetRequiredService<IPermissionSecurityEventWriter>());
        Assert.Equal("permission_denied", writer.LastRecord?.EventType);
        Assert.Equal(42, writer.LastRecord?.UserId);
        Assert.Contains(SystemPermissions.SecurePing, writer.LastRecord?.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InvokeAsync_ReturnsForbiddenWhenPermissionIsMissing()
    {
        var (httpContext, filterContext) = CreateContext(PermissionCheckResult.Forbidden, userId: 42);
        var filter = CreateFilter(httpContext);

        var result = await filter.InvokeAsync(filterContext, _ => throw new InvalidOperationException("Next must not run."));

        Assert.Equal(ApiCodes.ToHttpStatus(ApiCodes.Forbidden), await ExecuteStatusCodeAsync(result, httpContext));
    }

    [Fact]
    public async Task InvokeAsync_ReturnsPermissionDeniedWhenPermissionIsMissing()
    {
        var (httpContext, filterContext) = CreateContext(PermissionCheckResult.Forbidden, userId: 42);
        var filter = CreateFilter(httpContext);

        var result = await filter.InvokeAsync(filterContext, _ => throw new InvalidOperationException("Next must not run."));
        var response = await ExecuteResultAsync(result, httpContext);

        Assert.Equal(ApiCodes.ToHttpStatus(ApiCodes.Forbidden), response.StatusCode);
        Assert.Equal("Permission denied.", response.Message);
        var writer = Assert.IsType<FakePermissionSecurityEventWriter>(
            httpContext.RequestServices.GetRequiredService<IPermissionSecurityEventWriter>());
        Assert.Equal("permission_denied", writer.LastRecord?.EventType);
        Assert.Equal(42, writer.LastRecord?.UserId);
        Assert.Equal("trace-test", writer.LastRecord?.TraceId);
    }

    [Fact]
    public async Task InvokeAsync_ReturnsForbiddenWhenFileDownloadPermissionMissing()
    {
        var (httpContext, filterContext) = CreateContext(PermissionCheckResult.Forbidden, FilePermissions.Download, userId: 42);
        var filter = CreateFilter(httpContext);

        var result = await filter.InvokeAsync(filterContext, _ => throw new InvalidOperationException("Next must not run."));

        Assert.Equal(ApiCodes.ToHttpStatus(ApiCodes.Forbidden), await ExecuteStatusCodeAsync(result, httpContext));
    }

    [Fact]
    public async Task InvokeAsync_CallsNextWhenPermissionIsAllowed()
    {
        var (httpContext, filterContext) = CreateContext(PermissionCheckResult.Allowed, userId: 42);
        var filter = CreateFilter(httpContext);
        var called = false;

        var result = await filter.InvokeAsync(filterContext, _ =>
        {
            called = true;

            return ValueTask.FromResult<object?>("allowed");
        });

        Assert.True(called);
        Assert.Equal("allowed", result);
        var checker = Assert.IsType<FakePermissionChecker>(
            httpContext.RequestServices.GetRequiredService<IPermissionChecker>());
        Assert.Equal(42, checker.LastUserId);
        Assert.Equal(SystemPermissions.SecurePing, checker.LastPermissionCode);
    }

    [Fact]
    public async Task InvokeAsync_CallsNextWhenFilePermissionIsAllowed()
    {
        var (httpContext, filterContext) = CreateContext(PermissionCheckResult.Allowed, FilePermissions.Download, userId: 42);
        var filter = CreateFilter(httpContext);
        var called = false;

        var result = await filter.InvokeAsync(filterContext, _ =>
        {
            called = true;

            return ValueTask.FromResult<object?>("allowed");
        });

        Assert.True(called);
        Assert.Equal("allowed", result);
        var checker = Assert.IsType<FakePermissionChecker>(
            httpContext.RequestServices.GetRequiredService<IPermissionChecker>());
        Assert.Equal(42, checker.LastUserId);
        Assert.Equal(FilePermissions.Download, checker.LastPermissionCode);
    }

    private static (DefaultHttpContext HttpContext, EndpointFilterInvocationContext FilterContext) CreateContext(
        PermissionCheckResult result,
        long? userId)
    {
        return CreateContext(result, SystemPermissions.SecurePing, userId);
    }

    private static (DefaultHttpContext HttpContext, EndpointFilterInvocationContext FilterContext) CreateContext(
        PermissionCheckResult result,
        string permissionCode,
        long? userId)
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IPermissionChecker>(new FakePermissionChecker(result))
            .AddSingleton<IPermissionSecurityEventWriter, FakePermissionSecurityEventWriter>()
            .AddSingleton<IAuthClock>(new FakeAuthClock(new DateTimeOffset(2026, 6, 19, 0, 0, 0, TimeSpan.Zero)))
            .BuildServiceProvider();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = services,
            TraceIdentifier = "trace-test"
        };
        httpContext.Response.Body = new MemoryStream();
        httpContext.SetEndpoint(new Endpoint(
            _ => Task.CompletedTask,
            new EndpointMetadataCollection(new PermissionMetadata(permissionCode)),
            "secure-ping-test"));

        if (userId is not null)
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString(System.Globalization.CultureInfo.InvariantCulture))],
                authenticationType: "unit-test"));
        }

        return (httpContext, new TestEndpointFilterInvocationContext(httpContext));
    }

    private static PermissionEndpointFilter CreateFilter(DefaultHttpContext httpContext)
    {
        return new PermissionEndpointFilter(
            httpContext.RequestServices.GetRequiredService<IPermissionChecker>(),
            httpContext.RequestServices.GetRequiredService<IPermissionSecurityEventWriter>(),
            httpContext.RequestServices.GetRequiredService<IAuthClock>());
    }

    private static async Task<int> ExecuteStatusCodeAsync(object? result, DefaultHttpContext httpContext)
    {
        var response = await ExecuteResultAsync(result, httpContext);

        return response.StatusCode;
    }

    private static async Task<(int StatusCode, string? Message)> ExecuteResultAsync(
        object? result,
        DefaultHttpContext httpContext)
    {
        var typedResult = Assert.IsAssignableFrom<IResult>(result);
        await typedResult.ExecuteAsync(httpContext);
        httpContext.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(httpContext.Response.Body, default, TestContext.Current.CancellationToken);
        var message = document.RootElement.GetProperty("msg").GetString();

        return (httpContext.Response.StatusCode, message);
    }

    private sealed class TestEndpointFilterInvocationContext : EndpointFilterInvocationContext
    {
        public TestEndpointFilterInvocationContext(HttpContext httpContext)
        {
            HttpContext = httpContext;
        }

        public override HttpContext HttpContext { get; }

        public override IList<object?> Arguments { get; } = [];

        public override T GetArgument<T>(int index)
        {
            return (T)Arguments[index]!;
        }
    }

    private sealed class FakePermissionChecker : IPermissionChecker
    {
        private readonly PermissionCheckResult _result;

        public FakePermissionChecker(PermissionCheckResult result)
        {
            _result = result;
        }

        public long? LastUserId { get; private set; }

        public string? LastPermissionCode { get; private set; }

        public Task<PermissionCheckResult> CheckAsync(
            long userId,
            string permissionCode,
            CancellationToken cancellationToken)
        {
            LastUserId = userId;
            LastPermissionCode = permissionCode;

            return Task.FromResult(_result);
        }
    }

    private sealed class FakePermissionSecurityEventWriter : IPermissionSecurityEventWriter
    {
        public PermissionSecurityEventRecord? LastRecord { get; private set; }

        public Task RecordAsync(PermissionSecurityEventRecord record, CancellationToken cancellationToken)
        {
            LastRecord = record;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeAuthClock : IAuthClock
    {
        public FakeAuthClock(DateTimeOffset utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTimeOffset UtcNow { get; }
    }
}
