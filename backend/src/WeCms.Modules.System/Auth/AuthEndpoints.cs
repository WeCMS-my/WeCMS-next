using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WeCms.Shared;
using WeCms.Shared.Security;

#pragma warning disable IL2026, IL3050
// Minimal API MapGet/MapPost use delegate reflection — handled by ASP.NET Core source generators at publish time.

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

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        IAuthService authService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.Ok(ApiResult<LoginResponse>.Fail(
                ApiCodes.ValidationError, "用户名和密码不能为空"));
        }

        try
        {
            var result = await authService.LoginAsync(
                request,
                httpContext.GetClientIp(),
                httpContext.Request.Headers.UserAgent.ToString(),
                cancellationToken);

            return Results.Ok(ApiResult<LoginResponse>.Ok(result));
        }
        catch (DomainException ex)
        {
            return Results.Ok(ApiResult<LoginResponse>.Fail(ex.Code, ex.Message));
        }
    }

    private static async Task<IResult> RefreshAsync(
        RefreshRequest request,
        IAuthService authService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return Results.Ok(ApiResult<RefreshResponse>.Fail(
                ApiCodes.ValidationError, "刷新令牌不能为空"));
        }

        try
        {
            var result = await authService.RefreshAsync(
                request,
                httpContext.GetClientIp(),
                httpContext.Request.Headers.UserAgent.ToString(),
                cancellationToken);

            return Results.Ok(ApiResult<RefreshResponse>.Ok(result));
        }
        catch (DomainException ex)
        {
            return Results.Ok(ApiResult<RefreshResponse>.Fail(ex.Code, ex.Message));
        }
    }

    private static async Task<IResult> LogoutAsync(
        LogoutRequest request,
        IAuthService authService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return Results.Ok(ApiResult<object?>.Ok(null));

        await authService.LogoutAsync(request.RefreshToken, cancellationToken);
        return Results.Ok(ApiResult<object?>.Ok(null));
    }

    private static async Task<IResult> GetCurrentUserAsync(
        IAuthService authService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var subClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (subClaim is null || !long.TryParse(subClaim, out var userId))
        {
            return Results.Ok(ApiResult<CurrentUserResponse>.Fail(
                ApiCodes.Unauthorized, "未登录"));
        }

        try
        {
            var result = await authService.GetCurrentUserAsync(userId, cancellationToken);
            return Results.Ok(ApiResult<CurrentUserResponse>.Ok(result));
        }
        catch (DomainException ex)
        {
            return Results.Ok(ApiResult<CurrentUserResponse>.Fail(ex.Code, ex.Message));
        }
    }
}
