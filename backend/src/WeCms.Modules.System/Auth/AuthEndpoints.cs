using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using WeCms.Shared;
using WeCms.Shared.Security;

namespace WeCms.Modules.System.Auth;

/// <summary>
/// Auth endpoint handlers with strict constructor injection (no Service Locator).
/// </summary>
public sealed class AuthEndpointHandlers
{
    private readonly IAuthService _authService;

    public AuthEndpointHandlers(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task LoginAsync(HttpContext httpContext)
    {
        var cancellationToken = httpContext.RequestAborted;
        var request = await ParseRequestAsync<LoginRequest>(httpContext, cancellationToken);

        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new DomainException(ApiCodes.ValidationError, "用户名和密码不能为空");
        }

        var result = await _authService.LoginAsync(
            request,
            httpContext.GetClientIp(),
            httpContext.Request.Headers.UserAgent.ToString(),
            cancellationToken);

        await WriteJsonResponse(httpContext, ApiResult<LoginResponse>.Ok(result), typeof(ApiResult<LoginResponse>), cancellationToken);
    }

    public async Task RefreshAsync(HttpContext httpContext)
    {
        var cancellationToken = httpContext.RequestAborted;
        var request = await ParseRequestAsync<RefreshRequest>(httpContext, cancellationToken);

        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            throw new DomainException(ApiCodes.ValidationError, "刷新令牌不能为空");
        }

        var result = await _authService.RefreshAsync(
            request,
            httpContext.GetClientIp(),
            httpContext.Request.Headers.UserAgent.ToString(),
            cancellationToken);

        await WriteJsonResponse(httpContext, ApiResult<RefreshResponse>.Ok(result), typeof(ApiResult<RefreshResponse>), cancellationToken);
    }

    public async Task LogoutAsync(HttpContext httpContext)
    {
        var cancellationToken = httpContext.RequestAborted;
        var request = await ParseRequestAsync<LogoutRequest>(httpContext, cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            await _authService.LogoutAsync(request.RefreshToken, cancellationToken);
        }

        await WriteJsonResponse(httpContext, ApiResult<object?>.Ok(null), typeof(ApiResult<object?>), cancellationToken);
    }

    public async Task GetCurrentUserAsync(HttpContext httpContext)
    {
        var cancellationToken = httpContext.RequestAborted;
        var subClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (subClaim is null || !long.TryParse(subClaim, out var userId))
        {
            throw new DomainException(ApiCodes.Unauthorized, "未登录");
        }

        var result = await _authService.GetCurrentUserAsync(userId, cancellationToken);
        await WriteJsonResponse(httpContext, ApiResult<CurrentUserResponse>.Ok(result), typeof(ApiResult<CurrentUserResponse>), cancellationToken);
    }

    private static async Task<T> ParseRequestAsync<T>(HttpContext context, CancellationToken cancellationToken)
        where T : class
    {
        var request = await context.Request.ReadFromJsonAsync(typeof(T), WeCmsModulesSystemJsonContext.Default, cancellationToken);
        return (request as T)!;
    }

    private static async Task WriteJsonResponse(
        HttpContext context,
        object result,
        Type resultType,
        CancellationToken cancellationToken)
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        await context.Response.WriteAsJsonAsync(result, resultType, WeCmsModulesSystemJsonContext.Default, cancellationToken: cancellationToken);
    }
}

/// <summary>
/// Endpoint route registration — thin AOT-compatible RequestDelegate wrappers.
/// </summary>
public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth");

        ((RouteHandlerBuilder)group.MapPost("/login", (RequestDelegate)HandleLogin))
            .AllowAnonymous()
            .Accepts<LoginRequest>("application/json")
            .Produces<ApiResult<LoginResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResult<object?>>(StatusCodes.Status400BadRequest)
            .WithName("Auth_Login");

        ((RouteHandlerBuilder)group.MapPost("/refresh", (RequestDelegate)HandleRefresh))
            .AllowAnonymous()
            .Accepts<RefreshRequest>("application/json")
            .Produces<ApiResult<RefreshResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResult<object?>>(StatusCodes.Status400BadRequest)
            .WithName("Auth_Refresh");

        ((RouteHandlerBuilder)group.MapPost("/logout", (RequestDelegate)HandleLogout))
            .RequireAuthorization()
            .Accepts<LogoutRequest>("application/json")
            .Produces<ApiResult<object?>>(StatusCodes.Status200OK)
            .Produces<ApiResult<object?>>(StatusCodes.Status400BadRequest)
            .WithName("Auth_Logout");

        group.MapGet("/me", (RequestDelegate)HandleGetCurrentUser)
            .RequireAuthorization()
            .WithName("Auth_Me");

        return group;
    }

    private static Task HandleLogin(HttpContext context) =>
        context.RequestServices.GetRequiredService<AuthEndpointHandlers>().LoginAsync(context);

    private static Task HandleRefresh(HttpContext context) =>
        context.RequestServices.GetRequiredService<AuthEndpointHandlers>().RefreshAsync(context);

    private static Task HandleLogout(HttpContext context) =>
        context.RequestServices.GetRequiredService<AuthEndpointHandlers>().LogoutAsync(context);

    private static Task HandleGetCurrentUser(HttpContext context) =>
        context.RequestServices.GetRequiredService<AuthEndpointHandlers>().GetCurrentUserAsync(context);
}
