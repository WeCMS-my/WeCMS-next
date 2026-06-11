using System.Security.Claims;
using Microsoft.AspNetCore.Http;
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

    public async Task LoginAsync(LoginRequest request, HttpContext httpContext)
    {
        var cancellationToken = httpContext.RequestAborted;

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

    public async Task RefreshAsync(RefreshRequest request, HttpContext httpContext)
    {
        var cancellationToken = httpContext.RequestAborted;

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

    public async Task LogoutAsync(LogoutRequest request, HttpContext httpContext)
    {
        var cancellationToken = httpContext.RequestAborted;

        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            throw new DomainException(ApiCodes.ValidationError, "刷新令牌不能为空");
        }

        await _authService.LogoutAsync(request.RefreshToken, cancellationToken);

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
