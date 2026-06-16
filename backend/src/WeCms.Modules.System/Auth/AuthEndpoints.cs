using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WeCms.Shared;

namespace WeCms.Modules.System.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/auth");

        group.MapPost("/login", async (
                LoginRequest request,
                HttpContext context,
                IAuthService authService,
                CancellationToken cancellationToken) =>
            {
                var response = await authService.LoginAsync(request, CreateRequestContext(context), cancellationToken);
                return Results.Ok(ApiResult<LoginResponse>.Ok(response));
            })
            .WithMetadata(new OpenApiRequestBodyMetadata(typeof(LoginRequest)))
            .WithMetadata(new OpenApiResponseMetadata(typeof(LoginResponse)))
            .AllowAnonymous();

        group.MapPost("/refresh", async (
                RefreshTokenRequest request,
                HttpContext context,
                IAuthService authService,
                CancellationToken cancellationToken) =>
            {
                var response = await authService.RefreshAsync(request, CreateRequestContext(context), cancellationToken);
                return Results.Ok(ApiResult<LoginResponse>.Ok(response));
            })
            .WithMetadata(new OpenApiRequestBodyMetadata(typeof(RefreshTokenRequest)))
            .WithMetadata(new OpenApiResponseMetadata(typeof(LoginResponse)))
            .AllowAnonymous();

        group.MapPost("/logout", async (
                LogoutRequest request,
                HttpContext context,
                IAuthService authService,
                CancellationToken cancellationToken) =>
            {
                await authService.LogoutAsync(request, CreateRequestContext(context), cancellationToken);
                return Results.Ok(ApiResult<object?>.Ok(null));
            })
            .WithMetadata(new OpenApiRequestBodyMetadata(typeof(LogoutRequest)))
            .WithMetadata(new OpenApiResponseMetadata(typeof(object)))
            .RequireAuthorization();

        group.MapGet("/me", async (
                ClaimsPrincipal principal,
                IAuthService authService,
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

    private static AuthRequestContext CreateRequestContext(HttpContext context)
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
        var userAgent = context.Request.Headers.UserAgent.ToString();

        return new AuthRequestContext(ip, userAgent);
    }
}
