using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using WeCms.Shared;
using WeCms.Shared.Endpoints;

namespace WeCms.Tests.Unit.Api;

public sealed class EndpointPermissionFilterTests
{
    private const string PermissionCode = "sys:user:create";

    [Fact]
    public async Task InvokeAsync_CallsNextWhenPermissionIsAllowed()
    {
        var (httpContext, filterContext, checker, _) = CreateContext(EndpointPermissionCheckResult.Allowed, userId: 42);
        var filter = new EndpointPermissionFilter(checker, new FakeDeniedRecorder());
        var called = false;

        var result = await filter.InvokeAsync(filterContext, _ =>
        {
            called = true;
            return ValueTask.FromResult<object?>("allowed");
        });

        Assert.True(called);
        Assert.Equal("allowed", result);
        Assert.Equal(42, checker.LastUserId);
        Assert.Equal(PermissionCode, checker.LastPermissionCode);
        Assert.Equal(httpContext.RequestAborted, checker.LastCancellationToken);
    }

    [Fact]
    public async Task InvokeAsync_ReturnsUnauthorizedWhenUserIsMissing()
    {
        var (_, filterContext, checker, recorder) = CreateContext(EndpointPermissionCheckResult.Allowed, userId: null);
        var filter = new EndpointPermissionFilter(checker, recorder);

        var result = await filter.InvokeAsync(filterContext, _ => throw new InvalidOperationException("Next must not run."));
        var response = await ExecuteResultAsync(result, filterContext.HttpContext);

        Assert.Equal(ApiCodes.ToHttpStatus(ApiCodes.Unauthorized), response.StatusCode);
        Assert.Equal("Authentication is required.", response.Message);
        Assert.Null(recorder.LastRecord);
    }

    [Fact]
    public async Task InvokeAsync_ReturnsUnauthorizedAndRecordsWhenUserIsDisabled()
    {
        var (_, filterContext, checker, recorder) = CreateContext(EndpointPermissionCheckResult.UserDisabled, userId: 42);
        var filter = new EndpointPermissionFilter(checker, recorder);

        var result = await filter.InvokeAsync(filterContext, _ => throw new InvalidOperationException("Next must not run."));
        var response = await ExecuteResultAsync(result, filterContext.HttpContext);

        Assert.Equal(ApiCodes.ToHttpStatus(ApiCodes.Unauthorized), response.StatusCode);
        Assert.Equal("User account is disabled.", response.Message);
        Assert.Equal(42, recorder.LastRecord?.UserId);
        Assert.Equal(PermissionCode, recorder.LastRecord?.PermissionCode);
        Assert.Equal("User account is disabled.", recorder.LastRecord?.Reason);
    }

    [Fact]
    public async Task InvokeAsync_ReturnsForbiddenAndRecordsWhenPermissionIsMissing()
    {
        var (_, filterContext, checker, recorder) = CreateContext(EndpointPermissionCheckResult.Forbidden, userId: 42);
        var filter = new EndpointPermissionFilter(checker, recorder);

        var result = await filter.InvokeAsync(filterContext, _ => throw new InvalidOperationException("Next must not run."));
        var response = await ExecuteResultAsync(result, filterContext.HttpContext);

        Assert.Equal(ApiCodes.ToHttpStatus(ApiCodes.Forbidden), response.StatusCode);
        Assert.Equal("Permission denied.", response.Message);
        Assert.Equal(42, recorder.LastRecord?.UserId);
        Assert.Equal(PermissionCode, recorder.LastRecord?.PermissionCode);
        Assert.Equal("Permission denied.", recorder.LastRecord?.Reason);
    }

    private static (
        DefaultHttpContext HttpContext,
        EndpointFilterInvocationContext FilterContext,
        FakeEndpointPermissionChecker Checker,
        FakeDeniedRecorder Recorder) CreateContext(
            EndpointPermissionCheckResult result,
            long? userId)
    {
        var httpContext = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider(),
            TraceIdentifier = "trace-test"
        };
        httpContext.Response.Body = new MemoryStream();
        httpContext.SetEndpoint(new Endpoint(
            _ => Task.CompletedTask,
            new EndpointMetadataCollection(new EndpointPermissionMetadata(PermissionCode, EndpointPermissionKind.Api)),
            "endpoint-permission-test"));

        if (userId is not null)
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString(System.Globalization.CultureInfo.InvariantCulture))],
                authenticationType: "unit-test"));
        }

        var checker = new FakeEndpointPermissionChecker(result);
        var recorder = new FakeDeniedRecorder();

        return (httpContext, new TestEndpointFilterInvocationContext(httpContext), checker, recorder);
    }

    private static async Task<(int StatusCode, string? Message)> ExecuteResultAsync(
        object? result,
        HttpContext httpContext)
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

    private sealed class FakeEndpointPermissionChecker : IEndpointPermissionChecker
    {
        private readonly EndpointPermissionCheckResult _result;

        public FakeEndpointPermissionChecker(EndpointPermissionCheckResult result)
        {
            _result = result;
        }

        public long? LastUserId { get; private set; }

        public string? LastPermissionCode { get; private set; }

        public CancellationToken LastCancellationToken { get; private set; }

        public Task<EndpointPermissionCheckResult> CheckAsync(
            long userId,
            string permissionCode,
            CancellationToken cancellationToken)
        {
            LastUserId = userId;
            LastPermissionCode = permissionCode;
            LastCancellationToken = cancellationToken;

            return Task.FromResult(_result);
        }
    }

    private sealed class FakeDeniedRecorder : IEndpointPermissionDeniedRecorder
    {
        public DeniedRecord? LastRecord { get; private set; }

        public Task RecordAsync(
            long userId,
            string? username,
            string permissionCode,
            string ip,
            string reason,
            string traceId,
            CancellationToken cancellationToken)
        {
            LastRecord = new DeniedRecord(userId, permissionCode, reason, traceId);
            return Task.CompletedTask;
        }
    }

    private sealed record DeniedRecord(
        long UserId,
        string PermissionCode,
        string Reason,
        string TraceId);
}
