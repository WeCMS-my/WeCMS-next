using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using WeCms.Modules.System.Security;
using WeCms.Shared;

namespace WeCms.Modules.System.Auth;

public static class AccountProfileEndpoints
{
    public static IEndpointRouteBuilder MapAccountProfileEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/account").RequireAuthorization();

        group.MapGet("/profile", GetProfileAsync)
            .WithMetadata(new OpenApiResponseMetadata(typeof(AccountProfileResponse)));
        group.MapPut("/profile", UpdateProfileAsync)
            .WithMetadata(new OpenApiRequestBodyMetadata(typeof(UpdateAccountProfileRequest)))
            .WithMetadata(new OpenApiResponseMetadata(typeof(AccountProfileResponse)));
        group.MapPut("/password", ChangePasswordAsync)
            .WithMetadata(new OpenApiRequestBodyMetadata(typeof(ChangeAccountPasswordRequest)))
            .WithMetadata(new OpenApiResponseMetadata(typeof(object)));
        group.MapPost("/avatar", UploadAvatarAsync)
            .Accepts<AccountAvatarUploadRequest>("multipart/form-data")
            .WithMetadata(new OpenApiRequestBodyMetadata(typeof(AccountAvatarUploadRequest)))
            .WithMetadata(new OpenApiResponseMetadata(typeof(AccountAvatarResponse)))
            .RequireRateLimiting(RateLimitPolicyNames.FileUpload);
        group.MapGet("/avatar/content", GetAvatarAsync);
        group.MapGet("/security", GetSecurityAsync)
            .WithMetadata(new OpenApiResponseMetadata(typeof(AccountSecurityResponse)));

        return endpoints;
    }

    private static async Task<ApiResult<AccountProfileResponse>> GetProfileAsync(HttpContext httpContext, IAccountProfileService service, IAuthClock clock, CancellationToken cancellationToken)
    {
        return ApiResult<AccountProfileResponse>.Ok(await service.GetProfileAsync(Context(httpContext, clock), cancellationToken));
    }

    private static async Task<ApiResult<AccountProfileResponse>> UpdateProfileAsync(UpdateAccountProfileRequest request, HttpContext httpContext, IAccountProfileService service, IAuthClock clock, CancellationToken cancellationToken)
    {
        return ApiResult<AccountProfileResponse>.Ok(await service.UpdateProfileAsync(request, Context(httpContext, clock), cancellationToken));
    }

    private static async Task<ApiResult<object>> ChangePasswordAsync(ChangeAccountPasswordRequest request, HttpContext httpContext, IAccountProfileService service, IAuthClock clock, CancellationToken cancellationToken)
    {
        await service.ChangePasswordAsync(request, Context(httpContext, clock), cancellationToken);
        return ApiResult<object>.Ok(new { });
    }

    private static async Task<ApiResult<AccountAvatarResponse>> UploadAvatarAsync(
        [FromForm] AccountAvatarUploadRequest request,
        [FromForm] IFormFile file,
        HttpContext httpContext,
        IAccountProfileService service,
        IAuthClock clock,
        CancellationToken cancellationToken)
    {
        return ApiResult<AccountAvatarResponse>.Ok(await service.UploadAvatarAsync(request, file, Context(httpContext, clock), cancellationToken));
    }

    private static async Task<IResult> GetAvatarAsync(HttpContext httpContext, IAccountProfileService service, IAuthClock clock, CancellationToken cancellationToken)
    {
        var payload = await service.GetAvatarAsync(Context(httpContext, clock), cancellationToken);
        httpContext.Response.Headers.XContentTypeOptions = "nosniff";
        httpContext.Response.Headers.ContentDisposition = $"inline; filename*=UTF-8''{Uri.EscapeDataString(payload.FileName)}";
        return Results.File(payload.Content, payload.ContentType, enableRangeProcessing: true);
    }

    private static async Task<ApiResult<AccountSecurityResponse>> GetSecurityAsync(HttpContext httpContext, IAccountProfileService service, IAuthClock clock, CancellationToken cancellationToken)
    {
        return ApiResult<AccountSecurityResponse>.Ok(await service.GetSecurityAsync(Context(httpContext, clock), cancellationToken));
    }

    private static AccountRequestContext Context(HttpContext httpContext, IAuthClock clock)
    {
        var userIdText = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(userIdText, out var userId))
        {
            throw new DomainException(ApiCodes.Unauthorized, "Authentication is required.");
        }

        return new AccountRequestContext(
            userId,
            httpContext.User.Identity?.Name ?? string.Empty,
            httpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
            httpContext.Request.Headers.UserAgent.ToString(),
            httpContext.TraceIdentifier,
            clock.UtcNow);
    }
}
