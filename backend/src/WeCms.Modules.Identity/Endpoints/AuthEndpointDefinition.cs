using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc;
using WeCms.Shared;
using WeCms.Shared.Endpoints;

namespace WeCms.Modules.Identity.Endpoints;

public sealed class AuthEndpointDefinition : IEndpointDefinition
{
    private const string RefreshCookieName = "__Host-wecms_refresh";

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapGroup("/api/v1/auth")
            .WithEndpointModule("identity")
            .AuditWriteEndpoints("identity", "auth");

        group.MapPost("/login", async (
                LoginRequest request,
                HttpContext context,
                [FromServices] IAuthService authService,
                CancellationToken cancellationToken) =>
            {
                var session = await authService.LoginAsync(request, CreateRequestContext(context), cancellationToken);
                if (!session.Response.RequiresTwoFactor)
                {
                    AppendRefreshTokenCookie(context, session);
                }

                return Results.Ok(ApiResult<LoginResponse>.Ok(session.Response));
            })
            .WithMetadata(new OpenApiRequestBodyMetadata(typeof(LoginRequest)))
            .WithMetadata(new OpenApiResponseMetadata(typeof(LoginResponse)))
            .RequireRateLimiting(IdentityEndpointRateLimitPolicyNames.AuthLogin)
            .AllowAnonymous();

        group.MapPost("/refresh", async (
                HttpContext context,
                [FromServices] IAuthService authService,
                [FromServices] IIdentityCookieAuthOriginValidator cookieAuthOriginValidator,
                CancellationToken cancellationToken) =>
            {
                await cookieAuthOriginValidator.ValidateAsync(context, IdentityCookieAuthOriginEndpoints.Refresh, CreateRequestContext(context), cancellationToken);
                var refreshToken = ReadRefreshTokenCookie(context);
                var session = await authService.RefreshAsync(refreshToken, CreateRequestContext(context), cancellationToken);
                AppendRefreshTokenCookie(context, session);
                return Results.Ok(ApiResult<LoginResponse>.Ok(session.Response));
            })
            .WithMetadata(new OpenApiResponseMetadata(typeof(LoginResponse)))
            .RequireRateLimiting(IdentityEndpointRateLimitPolicyNames.AuthRefresh)
            .AllowAnonymous();

        group.MapPost("/logout", async (
                HttpContext context,
                [FromServices] IAuthService authService,
                [FromServices] IIdentityCookieAuthOriginValidator cookieAuthOriginValidator,
                CancellationToken cancellationToken) =>
            {
                await cookieAuthOriginValidator.ValidateAsync(context, IdentityCookieAuthOriginEndpoints.Logout, CreateRequestContext(context), cancellationToken);
                var refreshToken = ReadRefreshTokenCookie(context);
                await authService.LogoutAsync(refreshToken, CreateRequestContext(context), cancellationToken);
                DeleteRefreshTokenCookie(context);
                return Results.Ok(ApiResult<object?>.Ok(null));
            })
            .WithMetadata(new OpenApiResponseMetadata(typeof(object)))
            .AllowAnonymous();

        group.MapPost("/2fa/verify", async (
                TwoFactorVerifyRequest request,
                HttpContext context,
                [FromServices] IAuthService authService,
                [FromServices] IIdentityCookieAuthOriginValidator cookieAuthOriginValidator,
                CancellationToken cancellationToken) =>
            {
                var requestContext = CreateRequestContext(context);
                await cookieAuthOriginValidator.ValidateAsync(context, IdentityCookieAuthOriginEndpoints.TwoFactorVerify, requestContext, cancellationToken);
                var session = await authService.VerifyTwoFactorAsync(request, requestContext, cancellationToken);
                AppendRefreshTokenCookie(context, session);
                return Results.Ok(ApiResult<LoginResponse>.Ok(session.Response));
            })
            .WithMetadata(new OpenApiRequestBodyMetadata(typeof(TwoFactorVerifyRequest)))
            .WithMetadata(new OpenApiResponseMetadata(typeof(LoginResponse)))
            .RequireRateLimiting(IdentityEndpointRateLimitPolicyNames.AuthTwoFactor)
            .AllowAnonymous();

        group.MapPost("/2fa/recovery-code", async (
                TwoFactorRecoveryCodeRequest request,
                HttpContext context,
                [FromServices] IAuthService authService,
                [FromServices] IIdentityCookieAuthOriginValidator cookieAuthOriginValidator,
                CancellationToken cancellationToken) =>
            {
                var requestContext = CreateRequestContext(context);
                await cookieAuthOriginValidator.ValidateAsync(context, IdentityCookieAuthOriginEndpoints.TwoFactorRecoveryCode, requestContext, cancellationToken);
                var session = await authService.VerifyTwoFactorRecoveryCodeAsync(request, requestContext, cancellationToken);
                AppendRefreshTokenCookie(context, session);
                return Results.Ok(ApiResult<LoginResponse>.Ok(session.Response));
            })
            .WithMetadata(new OpenApiRequestBodyMetadata(typeof(TwoFactorRecoveryCodeRequest)))
            .WithMetadata(new OpenApiResponseMetadata(typeof(LoginResponse)))
            .RequireRateLimiting(IdentityEndpointRateLimitPolicyNames.AuthTwoFactor)
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
        if (string.IsNullOrWhiteSpace(session.RefreshToken))
        {
            throw new DomainException(ApiCodes.Unauthorized, "Refresh token was not issued.");
        }

        context.Response.Cookies.Append(RefreshCookieName, session.RefreshToken, RefreshCookieOptionsFactory.CreateAppendOptions(session));
    }

    private static void DeleteRefreshTokenCookie(HttpContext context)
    {
        context.Response.Cookies.Delete(RefreshCookieName, RefreshCookieOptionsFactory.CreateDeleteOptions());
    }

    private static class RefreshCookieOptionsFactory
    {
        public static CookieOptions CreateAppendOptions(AuthSessionResult session)
        {
            var options = CreateBaseOptions();
            options.Expires = session.RefreshTokenExpiresAt;
            options.MaxAge = session.RefreshTokenMaxAge;
            return options;
        }

        public static CookieOptions CreateDeleteOptions()
        {
            return CreateBaseOptions();
        }

        private static CookieOptions CreateBaseOptions()
        {
            return new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/"
            };
        }
    }

    private static AuthRequestContext CreateRequestContext(HttpContext context)
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
        var userAgent = context.Request.Headers.UserAgent.ToString();
        var traceId = context.TraceIdentifier;

        return new AuthRequestContext(ip, userAgent, traceId);
    }
}
