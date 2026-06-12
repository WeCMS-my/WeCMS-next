using System.Security.Claims;
using WeCms.Shared;

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

    public async Task<ApiResult<LoginResponse>> LoginAsync(
        LoginRequest request,
        string ipAddress,
        string userAgent,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new DomainException(ApiCodes.ValidationError, "用户名和密码不能为空");
        }

        var result = await _authService.LoginAsync(
            request,
            ipAddress,
            userAgent,
            cancellationToken);

        return ApiResult<LoginResponse>.Ok(result);
    }

    public async Task<ApiResult<CaptchaChallengeResponse>> CreateCaptchaAsync(CancellationToken cancellationToken)
    {
        var result = await _authService.CreateCaptchaAsync(cancellationToken);
        return ApiResult<CaptchaChallengeResponse>.Ok(result);
    }

    public async Task<ApiResult<RefreshResponse>> RefreshAsync(
        RefreshRequest request,
        string ipAddress,
        string userAgent,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            throw new DomainException(ApiCodes.ValidationError, "刷新令牌不能为空");
        }

        var result = await _authService.RefreshAsync(
            request,
            ipAddress,
            userAgent,
            cancellationToken);

        return ApiResult<RefreshResponse>.Ok(result);
    }

    public async Task<ApiResult<object?>> LogoutAsync(LogoutRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            throw new DomainException(ApiCodes.ValidationError, "刷新令牌不能为空");
        }

        await _authService.LogoutAsync(request.RefreshToken, cancellationToken);

        return ApiResult<object?>.Ok(null);
    }

    public async Task<ApiResult<VerifyTwoFactorResponse>> VerifyTwoFactorAsync(
        VerifyTwoFactorRequest request,
        string ipAddress,
        string userAgent,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ChallengeId) || string.IsNullOrWhiteSpace(request.Code))
        {
            throw new DomainException(ApiCodes.ValidationError, "二次验证码不能为空");
        }

        var result = await _authService.VerifyTwoFactorAsync(request, ipAddress, userAgent, cancellationToken);
        return ApiResult<VerifyTwoFactorResponse>.Ok(result);
    }

    public async Task<ApiResult<CurrentUserResponse>> GetCurrentUserAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var subClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (subClaim is null || !long.TryParse(subClaim, out var userId))
        {
            throw new DomainException(ApiCodes.Unauthorized, "未登录");
        }

        var result = await _authService.GetCurrentUserAsync(userId, cancellationToken);
        return ApiResult<CurrentUserResponse>.Ok(result);
    }
}
