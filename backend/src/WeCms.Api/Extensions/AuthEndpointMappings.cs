using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using WeCms.Modules.System.Auth;
using WeCms.Shared;
using WeCms.Shared.Security;

namespace WeCms.Api.Extensions;

public static class AuthEndpointMappings
{
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Minimal API registration is validated by integration tests and AOT publish.")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Minimal API registration is validated by integration tests and AOT publish.")]
    public static RouteGroupBuilder MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/auth");

        var login = (RouteHandlerBuilder)group.MapPost("/login", HandleLoginAsync);
        login.AllowAnonymous();
        login.Produces<ApiResult<LoginResponse>>(StatusCodes.Status200OK);
        login.Produces<ApiResult<object?>>(StatusCodes.Status400BadRequest);
        login.WithName("Auth_Login");

        var refresh = (RouteHandlerBuilder)group.MapPost("/refresh", HandleRefreshAsync);
        refresh.AllowAnonymous();
        refresh.Produces<ApiResult<RefreshResponse>>(StatusCodes.Status200OK);
        refresh.Produces<ApiResult<object?>>(StatusCodes.Status400BadRequest);
        refresh.WithName("Auth_Refresh");

        var logout = (RouteHandlerBuilder)group.MapPost("/logout", HandleLogoutAsync);
        logout.RequireAuthorization();
        logout.Produces<ApiResult<object?>>(StatusCodes.Status200OK);
        logout.Produces<ApiResult<object?>>(StatusCodes.Status400BadRequest);
        logout.WithName("Auth_Logout");

        var me = (RouteHandlerBuilder)group.MapGet("/me", HandleCurrentUserAsync);
        me.RequireAuthorization();
        me.Produces<ApiResult<CurrentUserResponse>>(StatusCodes.Status200OK);
        me.Produces<ApiResult<object?>>(StatusCodes.Status401Unauthorized);
        me.WithName("Auth_Me");

        return group;
    }

    private static async Task<IResult> HandleLoginAsync(
        [FromBody] LoginRequest request,
        HttpContext context,
        [FromServices] AuthEndpointHandlers handlers,
        CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await handlers.LoginAsync(
            request,
            context.GetClientIp(),
            context.Request.Headers.UserAgent.ToString(),
            cancellationToken));
    }

    private static async Task<IResult> HandleRefreshAsync(
        [FromBody] RefreshRequest request,
        HttpContext context,
        [FromServices] AuthEndpointHandlers handlers,
        CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await handlers.RefreshAsync(
            request,
            context.GetClientIp(),
            context.Request.Headers.UserAgent.ToString(),
            cancellationToken));
    }

    private static async Task<IResult> HandleLogoutAsync(
        [FromBody] LogoutRequest request,
        [FromServices] AuthEndpointHandlers handlers,
        CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await handlers.LogoutAsync(request, cancellationToken));
    }

    private static async Task<IResult> HandleCurrentUserAsync(
        HttpContext context,
        [FromServices] AuthEndpointHandlers handlers,
        CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await handlers.GetCurrentUserAsync(context.User, cancellationToken));
    }
}
