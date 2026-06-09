using WeCms.Shared.Contracts;
using Microsoft.AspNetCore.Http;

namespace WeCms.Infrastructure.Security;

public sealed class CurrentUserProvider : ICurrentUser
{
    private readonly IHttpContextAccessor _http;

    public CurrentUserProvider(IHttpContextAccessor http) => _http = http;

    public long UserId
    {
        get
        {
            var s = _http.HttpContext?.User.FindFirst("sub")?.Value;
            return s is not null && long.TryParse(s, out var id) ? id : 0;
        }
    }

    public string Username =>
        _http.HttpContext?.User.FindFirst("username")?.Value ?? "";

    public string? IpAddress
    {
        get
        {
            var fwd = _http.HttpContext?.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            return fwd?.Split(',')[0].Trim()
                ?? _http.HttpContext?.Connection.RemoteIpAddress?.ToString();
        }
    }

    public bool IsSuperAdmin =>
        _http.HttpContext?.User.FindFirst("is_super_admin")?.Value == "true";
}
