using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using WeCms.Shared.Security;
using PermissionEndpointFilterHelper = WeCms.Tests.Unit.Permissions.PermissionEndpointFilterTestable;

namespace WeCms.Tests.Unit.Permissions;

public sealed class PermissionEndpointFilterTests
{
    [Fact]
    public async Task InvokeAsync_ShouldShortCircuit_WhenUserNotAuthenticated()
    {
        var context = CreateContext(authenticated: false);
        var checker = new TestPermissionChecker(new PermissionCheckResult(false, false));
        var filter = new PermissionEndpointFilterHelper(checker);
        var nextCalled = false;

        var result = await filter.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(Results.Ok());
        });

        Assert.False(nextCalled, "Filter should short-circuit when user is not authenticated");
        Assert.NotNull(result);
    }

    [Fact]
    public async Task InvokeAsync_ShouldShortCircuit_WhenUserHasNoPermission()
    {
        var context = CreateContext(authenticated: true);
        var checker = new TestPermissionChecker(new PermissionCheckResult(true, false));
        var filter = new PermissionEndpointFilterHelper(checker);
        var nextCalled = false;

        var result = await filter.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(Results.Ok());
        });

        Assert.False(nextCalled, "Filter should short-circuit when user has no permission");
        Assert.NotNull(result);
    }

    [Fact]
    public async Task InvokeAsync_ShouldShortCircuit_WhenUserDisabled()
    {
        var context = CreateContext(authenticated: true);
        var checker = new TestPermissionChecker(new PermissionCheckResult(false, true));
        var filter = new PermissionEndpointFilterHelper(checker);
        var nextCalled = false;

        var result = await filter.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(Results.Ok());
        });

        Assert.False(nextCalled, "Filter should short-circuit when user is disabled");
        Assert.NotNull(result);
    }

    [Fact]
    public async Task InvokeAsync_ShouldCallNext_WhenUserHasPermission()
    {
        var context = CreateContext(authenticated: true);
        var checker = new TestPermissionChecker(new PermissionCheckResult(true, true));
        var filter = new PermissionEndpointFilterHelper(checker);
        var nextCalled = false;

        var result = await filter.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            var ok = Results.Ok(new { status = "ok" });
            return ValueTask.FromResult<object?>(ok);
        });

        Assert.True(nextCalled, "Filter should call next when user has permission");
    }

    [Fact]
    public async Task InvokeAsync_ShouldCallNext_WhenNoPermissionMetadata()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
        var endpoint = new Endpoint(null!, new EndpointMetadataCollection(), "test-no-metadata");
        httpContext.SetEndpoint(endpoint);
        var context = new DefaultEndpointFilterInvocationContext(httpContext);
        var checker = new TestPermissionChecker(new PermissionCheckResult(false, false));
        var filter = new PermissionEndpointFilterHelper(checker);
        var nextCalled = false;

        var result = await filter.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(Results.Ok());
        });

        Assert.True(nextCalled, "Filter should pass through when no PermissionMetadata");
    }

    private static DefaultEndpointFilterInvocationContext CreateContext(bool authenticated)
    {
        var httpContext = new DefaultHttpContext();

        if (authenticated)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, "1"),
            };
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        }
        else
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
        }

        var endpoint = new Endpoint(null!, new EndpointMetadataCollection(
            new PermissionMetadata("sys:system:secure-ping")), "test");
        httpContext.SetEndpoint(endpoint);

        return new DefaultEndpointFilterInvocationContext(httpContext);
    }
}

/// <summary>
/// Testable version of PermissionEndpointFilter with explicit IPermissionChecker injection.
/// </summary>
public sealed class PermissionEndpointFilterTestable
{
    private readonly IPermissionChecker _checker;

    public PermissionEndpointFilterTestable(IPermissionChecker checker)
    {
        _checker = checker;
    }

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var endpoint = context.HttpContext.GetEndpoint();
        var metadata = endpoint?.Metadata.GetMetadata<PermissionMetadata>();
        if (metadata is null)
            return await next(context);

        var httpContext = context.HttpContext;

        var subClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(subClaim) || !long.TryParse(subClaim, out var userId))
        {
            return Results.Json(
                new { code = 401, msg = "未登录", data = (object?)null },
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var result = await _checker.CheckAsync(userId, metadata.Code, httpContext.RequestAborted);

        if (!result.IsActive)
        {
            return Results.Json(
                new { code = 401, msg = "用户已被禁用", data = (object?)null },
                statusCode: StatusCodes.Status401Unauthorized);
        }

        if (!result.HasPermission)
        {
            return Results.Json(
                new { code = 403, msg = "无权限", data = (object?)null },
                statusCode: StatusCodes.Status403Forbidden);
        }

        return await next(context);
    }
}

internal sealed class TestPermissionChecker : IPermissionChecker
{
    private readonly PermissionCheckResult _result;

    public TestPermissionChecker(PermissionCheckResult result) => _result = result;

    public Task<PermissionCheckResult> CheckAsync(
        long userId,
        string permissionCode,
        CancellationToken cancellationToken)
        => Task.FromResult(_result);
}
