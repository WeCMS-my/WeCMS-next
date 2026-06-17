using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc;
using WeCms.Shared;

namespace WeCms.Modules.System.Auth;

public static class AuthEndpoints
{
    private const string RefreshCookieName = "__Host-wecms_refresh";

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/auth");

        group.MapPost("/login", async (
                LoginRequest request,
                HttpContext context,
                [FromServices] IAuthService authService,
                CancellationToken cancellationToken) =>
            {
                var session = await authService.LoginAsync(request, CreateRequestContext(context), cancellationToken);
                AppendRefreshTokenCookie(context, session);
                return Results.Ok(ApiResult<LoginResponse>.Ok(session.Response));
            })
            .WithMetadata(new OpenApiRequestBodyMetadata(typeof(LoginRequest)))
            .WithMetadata(new OpenApiResponseMetadata(typeof(LoginResponse)))
            .AllowAnonymous();

        group.MapPost("/refresh", async (
                HttpContext context,
                [FromServices] IAuthService authService,
                CancellationToken cancellationToken) =>
            {
                var refreshToken = ReadRefreshTokenCookie(context);
                var session = await authService.RefreshAsync(refreshToken, CreateRequestContext(context), cancellationToken);
                AppendRefreshTokenCookie(context, session);
                return Results.Ok(ApiResult<LoginResponse>.Ok(session.Response));
            })
            .WithMetadata(new OpenApiResponseMetadata(typeof(LoginResponse)))
            .AllowAnonymous();

        group.MapPost("/logout", async (
                HttpContext context,
                [FromServices] IAuthService authService,
                CancellationToken cancellationToken) =>
            {
                var refreshToken = ReadRefreshTokenCookie(context);
                await authService.LogoutAsync(refreshToken, CreateRequestContext(context), cancellationToken);
                DeleteRefreshTokenCookie(context);
                return Results.Ok(ApiResult<object?>.Ok(null));
            })
            .WithMetadata(new OpenApiResponseMetadata(typeof(object)))
            .AllowAnonymous();

        group.MapGet("/me", async (
                ClaimsPrincipal principal,
                [FromServices] IAuthService authService,
                CancellationToken cancellationToken) =>
            {
                var userIdValue = principal.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!long.TryParse(userIdValue, out var userId))
                {
                    throw new DomainException(ApiCodes.Unauthorized, "Authentication is required.");
                }

                var response = await authService.MeAsync(userId, cancellationToken);
                return Results.Ok(ApiResult<AuthMeResponse>.Ok(response));
            })
            .WithMetadata(new OpenApiResponseMetadata(typeof(AuthMeResponse)))
            .RequireAuthorization();

        return endpoints;
    }

    private static string ReadRefreshTokenCookie(HttpContext context)
    {
        if (!context.Request.Cookies.TryGetValue(RefreshCookieName, out var refreshToken)
            || string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new DomainException(ApiCodes.Unauthorized, "Invalid refresh token.");
        }

        return refreshToken;
    }

    private static void AppendRefreshTokenCookie(HttpContext context, AuthSessionResult session)
    {
        context.Response.Cookies.Append(RefreshCookieName, session.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            Expires = session.RefreshTokenExpiresAt,
            MaxAge = session.RefreshTokenMaxAge
        });
    }

    private static void DeleteRefreshTokenCookie(HttpContext context)
    {
        context.Response.Cookies.Delete(RefreshCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/"
        });
    }

    private static AuthRequestContext CreateRequestContext(HttpContext context)
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
        var userAgent = context.Request.Headers.UserAgent.ToString();
        var traceId = context.TraceIdentifier;

        return new AuthRequestContext(ip, userAgent, traceId);
    }
}
