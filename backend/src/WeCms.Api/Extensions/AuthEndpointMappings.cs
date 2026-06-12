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
        login.Produces<ApiResult<LoginResponse>>(StatusCodes.Status200OK);
        login.Produces<ApiResult<object?>>(StatusCodes.Status400BadRequest);
        login.WithName("Auth_Login");

        var refresh = (RouteHandlerBuilder)group.MapPost("/refresh", AuthEndpointRequestDelegates.HandleRefreshAsync);
        refresh.AllowAnonymous();
        refresh.Produces<ApiResult<RefreshResponse>>(StatusCodes.Status200OK);
        refresh.Produces<ApiResult<object?>>(StatusCodes.Status400BadRequest);
        refresh.WithName("Auth_Refresh");

        var logout = (RouteHandlerBuilder)group.MapPost("/logout", AuthEndpointRequestDelegates.HandleLogoutAsync);
        logout.RequireAuthorization();
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
