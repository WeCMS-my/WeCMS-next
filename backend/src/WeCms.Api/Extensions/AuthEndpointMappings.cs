using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WeCms.Modules.System.Auth;
using WeCms.Shared;

namespace WeCms.Api.Extensions;

public static class AuthEndpointMappings
{
    public static RouteGroupBuilder MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/auth");

        var login = (RouteHandlerBuilder)group.MapPost("/login", AuthEndpointRequestDelegates.HandleLoginAsync);
        login.AllowAnonymous();
        login.Accepts<LoginRequest>("application/json");
        login.Produces<ApiResult<LoginResponse>>(StatusCodes.Status200OK);
        login.Produces<ApiResult<object?>>(StatusCodes.Status400BadRequest);
        login.WithName("Auth_Login");

        var captcha = (RouteHandlerBuilder)group.MapGet("/captcha", AuthEndpointRequestDelegates.HandleCaptchaAsync);
        captcha.AllowAnonymous();
        captcha.Produces<ApiResult<CaptchaChallengeResponse>>(StatusCodes.Status200OK);
        captcha.Produces<ApiResult<object?>>(StatusCodes.Status429TooManyRequests);
        captcha.WithName("Auth_Captcha");

        var refresh = (RouteHandlerBuilder)group.MapPost("/refresh", AuthEndpointRequestDelegates.HandleRefreshAsync);
        refresh.AllowAnonymous();
        refresh.Accepts<RefreshRequest>("application/json");
        refresh.Produces<ApiResult<RefreshResponse>>(StatusCodes.Status200OK);
        refresh.Produces<ApiResult<object?>>(StatusCodes.Status400BadRequest);
        refresh.WithName("Auth_Refresh");

        var verifyTwoFactor = (RouteHandlerBuilder)group.MapPost("/verify-2fa", AuthEndpointRequestDelegates.HandleVerifyTwoFactorAsync);
        verifyTwoFactor.AllowAnonymous();
        verifyTwoFactor.Accepts<VerifyTwoFactorRequest>("application/json");
        verifyTwoFactor.Produces<ApiResult<VerifyTwoFactorResponse>>(StatusCodes.Status200OK);
        verifyTwoFactor.Produces<ApiResult<object?>>(StatusCodes.Status400BadRequest);
        verifyTwoFactor.Produces<ApiResult<object?>>(StatusCodes.Status401Unauthorized);
        verifyTwoFactor.WithName("Auth_VerifyTwoFactor");

        var logout = (RouteHandlerBuilder)group.MapPost("/logout", AuthEndpointRequestDelegates.HandleLogoutAsync);
        logout.RequireAuthorization();
        logout.Accepts<LogoutRequest>("application/json");
        logout.Produces<ApiResult<object?>>(StatusCodes.Status200OK);
        logout.Produces<ApiResult<object?>>(StatusCodes.Status400BadRequest);
        logout.WithName("Auth_Logout");

        var me = (RouteHandlerBuilder)group.MapGet("/me", AuthEndpointRequestDelegates.HandleCurrentUserAsync);
        me.RequireAuthorization();
        me.Produces<ApiResult<CurrentUserResponse>>(StatusCodes.Status200OK);
        me.Produces<ApiResult<object?>>(StatusCodes.Status401Unauthorized);
        me.WithName("Auth_Me");

        return group;
    }
}
