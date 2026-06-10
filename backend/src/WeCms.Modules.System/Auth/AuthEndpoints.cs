using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using WeCms.Shared;
using WeCms.Shared.Security;

namespace WeCms.Modules.System.Auth;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth");

        group.MapPost("/login", LoginAsync)
            .AllowAnonymous()
            .WithName("Auth_Login");

        group.MapPost("/refresh", RefreshAsync)
            .AllowAnonymous()
            .WithName("Auth_Refresh");

        group.MapPost("/logout", LogoutAsync)
            .RequireAuthorization()
            .WithName("Auth_Logout");

        group.MapGet("/me", GetCurrentUserAsync)
            .RequireAuthorization()
            .WithName("Auth_Me");

        return group;
    }

    private static async Task LoginAsync(HttpContext httpContext)
    {
        var cancellationToken = httpContext.RequestAborted;
        var request = await ParseRequestAsync<LoginRequest>(httpContext, cancellationToken);

        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new DomainException(ApiCodes.ValidationError, "用户名和密码不能为空");
        }

        var authService = httpContext.RequestServices.GetRequiredService<IAuthService>();
        var result = await authService.LoginAsync(
            request,
            httpContext.GetClientIp(),
            httpContext.Request.Headers.UserAgent.ToString(),
            cancellationToken);

        await WriteJsonResponse(httpContext, ApiResult<LoginResponse>.Ok(result), typeof(ApiResult<LoginResponse>), cancellationToken);
    }

    private static async Task RefreshAsync(HttpContext httpContext)
    {
        var cancellationToken = httpContext.RequestAborted;
        var request = await ParseRequestAsync<RefreshRequest>(httpContext, cancellationToken);

        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            throw new DomainException(ApiCodes.ValidationError, "刷新令牌不能为空");
        }

        var authService = httpContext.RequestServices.GetRequiredService<IAuthService>();
        var result = await authService.RefreshAsync(
            request,
            httpContext.GetClientIp(),
            httpContext.Request.Headers.UserAgent.ToString(),
            cancellationToken);

        await WriteJsonResponse(httpContext, ApiResult<RefreshResponse>.Ok(result), typeof(ApiResult<RefreshResponse>), cancellationToken);
    }

    private static async Task LogoutAsync(HttpContext httpContext)
    {
        var cancellationToken = httpContext.RequestAborted;
        var request = await ParseRequestAsync<LogoutRequest>(httpContext, cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            var authService = httpContext.RequestServices.GetRequiredService<IAuthService>();
            await authService.LogoutAsync(request.RefreshToken, cancellationToken);
        }

        await WriteJsonResponse(httpContext, ApiResult<object?>.Ok(null), typeof(ApiResult<object?>), cancellationToken);
    }

    private static async Task GetCurrentUserAsync(HttpContext httpContext)
    {
        var cancellationToken = httpContext.RequestAborted;
        var subClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (subClaim is null || !long.TryParse(subClaim, out var userId))
        {
            throw new DomainException(ApiCodes.Unauthorized, "未登录");
        }

        var authService = httpContext.RequestServices.GetRequiredService<IAuthService>();
        var result = await authService.GetCurrentUserAsync(userId, cancellationToken);
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
