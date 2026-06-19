namespace WeCms.Api.Middleware;

public sealed class SecureHeadersMiddleware
{
    private const string ContentTypeOptionsHeader = "X-Content-Type-Options";
    private const string FrameOptionsHeader = "X-Frame-Options";
    private const string ReferrerPolicyHeader = "Referrer-Policy";
    private const string PermissionsPolicyHeader = "Permissions-Policy";
    private const string CspHeader = "Content-Security-Policy";
    private const string CspReportOnlyHeader = "Content-Security-Policy-Report-Only";
    private const string DefaultPermissionsPolicy = "geolocation=(), microphone=(), camera=()";
    private const string ProductionReportOnlyCsp = "default-src 'self'; object-src 'none'; base-uri 'self'; frame-ancestors 'none'";
    private const string ViteDevelopmentReportOnlyCsp = "default-src 'self'; connect-src 'self' ws: wss: http://localhost:* http://127.0.0.1:*; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data: blob:; font-src 'self' data:; object-src 'none'; base-uri 'self'; frame-ancestors 'none'";

    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;

    public SecureHeadersMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        _next = next;
        _configuration = configuration;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            ApplyHeaders(context.Response.Headers);
            return Task.CompletedTask;
        });

        await _next(context);

        if (context.Response.Headers.Count == 0 || !context.Response.Headers.ContainsKey(ContentTypeOptionsHeader))
        {
            try
            {
                ApplyHeaders(context.Response.Headers);
            }
            catch (InvalidOperationException)
            {
                // In test hosts, OnStarting may not run after body writes;
                // fall back to a direct set and ignore if the real server already started the response.
            }
        }
    }

    private void ApplyHeaders(IHeaderDictionary headers)
    {
        SetIfMissing(headers, ContentTypeOptionsHeader, "nosniff");
        SetIfMissing(headers, FrameOptionsHeader, "DENY");
        SetIfMissing(headers, ReferrerPolicyHeader, "no-referrer");
        SetIfMissing(headers, PermissionsPolicyHeader, ReadString("PermissionsPolicy", DefaultPermissionsPolicy));

        if (ReadBool("CspEnabled", defaultValue: false))
        {
            SetIfMissing(headers, CspHeader, ReadCsp());
        }

        if (ReadBool("CspReportOnlyEnabled", defaultValue: true))
        {
            SetIfMissing(headers, CspReportOnlyHeader, ReadReportOnlyCsp());
        }
    }

    private string ReadCsp()
    {
        var configured = ReadString("Csp", string.Empty);
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured.Trim();
        }

        return ProductionReportOnlyCsp;
    }

    private string ReadReportOnlyCsp()
    {
        var configured = ReadString("CspReportOnly", string.Empty);
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured.Trim();
        }

        return _environment.IsDevelopment()
            ? ViteDevelopmentReportOnlyCsp
            : ProductionReportOnlyCsp;
    }

    private bool ReadBool(string key, bool defaultValue)
    {
        var value = _configuration[$"Security:SecureHeaders:{key}"];
        return bool.TryParse(value, out var parsed) ? parsed : defaultValue;
    }

    private string ReadString(string key, string defaultValue)
    {
        return _configuration[$"Security:SecureHeaders:{key}"] ?? defaultValue;
    }

    private static void SetIfMissing(IHeaderDictionary headers, string name, string value)
    {
        if (!headers.ContainsKey(name))
        {
            headers[name] = value;
        }
    }
}
