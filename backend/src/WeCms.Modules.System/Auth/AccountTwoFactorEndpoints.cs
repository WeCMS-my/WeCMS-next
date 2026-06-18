using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using WeCms.Shared;

namespace WeCms.Modules.System.Auth;

public static class AccountTwoFactorEndpoints
{
    public static IEndpointRouteBuilder MapAccountTwoFactorEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/account/2fa").RequireAuthorization();

        group.MapGet("/status", async (
                ClaimsPrincipal principal,
                [FromServices] IAccountTwoFactorService service,
                CancellationToken cancellationToken) =>
            {
                var response = await service.StatusAsync(RequireUserId(principal), cancellationToken);
                return Results.Ok(ApiResult<AccountTwoFactorStatusResponse>.Ok(response));
            })
            .WithMetadata(new OpenApiResponseMetadata(typeof(AccountTwoFactorStatusResponse)));

        group.MapPost("/setup", async (
                ClaimsPrincipal principal,
                HttpContext context,
                [FromServices] IAccountTwoFactorService service,
                CancellationToken cancellationToken) =>
            {
                var response = await service.BeginSetupAsync(RequireUserId(principal), CreateRequestContext(context), cancellationToken);
                return Results.Ok(ApiResult<AccountTwoFactorSetupResponse>.Ok(response));
            })
            .WithMetadata(new OpenApiResponseMetadata(typeof(AccountTwoFactorSetupResponse)));

        group.MapPost("/confirm", async (
                AccountTwoFactorConfirmRequest request,
                ClaimsPrincipal principal,
                HttpContext context,
                [FromServices] IAccountTwoFactorService service,
                CancellationToken cancellationToken) =>
            {
                var response = await service.ConfirmAsync(RequireUserId(principal), request, CreateRequestContext(context), cancellationToken);
                return Results.Ok(ApiResult<AccountTwoFactorStatusResponse>.Ok(response));
            })
            .WithMetadata(new OpenApiRequestBodyMetadata(typeof(AccountTwoFactorConfirmRequest)))
            .WithMetadata(new OpenApiResponseMetadata(typeof(AccountTwoFactorStatusResponse)));

        group.MapPost("/disable", async (
                AccountTwoFactorDisableRequest request,
                ClaimsPrincipal principal,
                HttpContext context,
                [FromServices] IAccountTwoFactorService service,
                CancellationToken cancellationToken) =>
            {
                await service.DisableAsync(RequireUserId(principal), request, CreateRequestContext(context), cancellationToken);
                return Results.Ok(ApiResult<object?>.Ok(null));
            })
            .WithMetadata(new OpenApiRequestBodyMetadata(typeof(AccountTwoFactorDisableRequest)))
            .WithMetadata(new OpenApiResponseMetadata(typeof(object)));

        group.MapPost("/recovery-codes/regenerate", async (
                AccountTwoFactorRegenerateRecoveryCodesRequest request,
                ClaimsPrincipal principal,
                HttpContext context,
                [FromServices] IAccountTwoFactorService service,
                CancellationToken cancellationToken) =>
            {
                var response = await service.RegenerateRecoveryCodesAsync(RequireUserId(principal), request, CreateRequestContext(context), cancellationToken);
                return Results.Ok(ApiResult<AccountTwoFactorRecoveryCodesResponse>.Ok(response));
            })
            .WithMetadata(new OpenApiRequestBodyMetadata(typeof(AccountTwoFactorRegenerateRecoveryCodesRequest)))
            .WithMetadata(new OpenApiResponseMetadata(typeof(AccountTwoFactorRecoveryCodesResponse)));

        return endpoints;
    }

    private static long RequireUserId(ClaimsPrincipal principal)
    {
        var userIdValue = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(userIdValue, out var userId))
        {
            throw new DomainException(ApiCodes.Unauthorized, "Authentication is required.");
        }

        return userId;
    }

    private static AuthRequestContext CreateRequestContext(HttpContext context)
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
        var userAgent = context.Request.Headers.UserAgent.ToString();
        var traceId = context.TraceIdentifier;

        return new AuthRequestContext(ip, userAgent, traceId);
    }
}
