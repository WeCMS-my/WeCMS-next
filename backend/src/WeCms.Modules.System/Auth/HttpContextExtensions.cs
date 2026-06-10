using Microsoft.AspNetCore.Http;

namespace WeCms.Modules.System.Auth;

public static class HttpContextExtensions
{
    public static string GetClientIp(this HttpContext context)
    {
        var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded))
            return forwarded.Split(',')[0].Trim();

        return context.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";
    }
}
