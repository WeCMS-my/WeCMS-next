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
            .AllowAnonymous();

        group.MapPost("/logout", (LogoutRequest _) =>
            Task.FromException<IResult>(new DomainException(ApiCodes.BusinessError, "Logout token revocation is not part of this refresh token rotation task.")))
            .AllowAnonymous();

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
