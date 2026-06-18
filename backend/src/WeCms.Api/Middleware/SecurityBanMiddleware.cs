using System.Security.Claims;
using System.Text.Json;
using WeCms.Api.Json;
using WeCms.Modules.System.Auth;
using WeCms.Modules.System.Security;
using WeCms.Shared;

namespace WeCms.Api.Middleware;

public sealed class SecurityBanMiddleware
{
    private const string BlockedMessage = "Security policy blocks this request.";

    private readonly RequestDelegate _next;

    public SecurityBanMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ISecurityBanService securityBanService,
        IAuthClock clock)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(securityBanService);
        ArgumentNullException.ThrowIfNull(clock);

        var now = clock.UtcNow;
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
        var ban = string.IsNullOrWhiteSpace(ip)
            ? null
            : await securityBanService.FindActiveAsync(SecurityBanTypes.Ip, ip, now, context.RequestAborted);

        var userId = ReadUserId(context);
        if (ban is null && userId is not null)
        {
            ban = await securityBanService.FindActiveAsync(
                SecurityBanTypes.User,
                userId.Value.ToString(global::System.Globalization.CultureInfo.InvariantCulture),
                now,
                context.RequestAborted);
        }

        if (ban is null)
        {
            await _next(context);
            return;
        }

        await securityBanService.RecordHitAsync(
            ban,
            new SecurityBanHitContext(
                userId,
                context.User.Identity?.Name,
                ip,
                context.Request.Headers.UserAgent.ToString(),
                context.TraceIdentifier,
                now),
            context.RequestAborted);

        await WriteForbiddenAsync(context);
    }

    private static long? ReadUserId(HttpContext context)
    {
        var userIdText = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return long.TryParse(userIdText, out var userId) ? userId : null;
    }

    private static async Task WriteForbiddenAsync(HttpContext context)
    {
        if (context.Response.HasStarted)
        {
            throw new InvalidOperationException("Cannot write API error response after the response has started.");
        }

        context.Response.Clear();
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/json; charset=utf-8";

        var result = ApiResult<object>.Error(ApiCodes.Forbidden, BlockedMessage, context.TraceIdentifier);
        await JsonSerializer.SerializeAsync(
            context.Response.Body,
            result,
            WeCmsJsonSerializerContext.Default.ApiResultObject,
            context.RequestAborted);
    }
}
