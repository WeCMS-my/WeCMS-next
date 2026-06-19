using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace WeCms.Api.Security;

public static class WeCmsForwardedHeadersExtensions
{
    private const string SectionName = "Security:ForwardedHeaders";

    public static IServiceCollection AddWeCmsForwardedHeaders(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.RequireHeaderSymmetry = true;
            options.KnownProxies.Clear();
            options.KnownIPNetworks.Clear();

            foreach (var proxy in ReadValues(configuration, "KnownProxies"))
            {
                options.KnownProxies.Add(ParseIpAddress(proxy, "Security:ForwardedHeaders:KnownProxies"));
            }

            foreach (var network in ReadValues(configuration, "KnownNetworks"))
            {
                options.KnownIPNetworks.Add(ParseNetwork(network));
            }
        });

        return services;
    }

    public static IApplicationBuilder UseWeCmsForwardedHeaders(
        this IApplicationBuilder app,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(configuration);

        return IsEnabled(configuration)
            ? app.UseForwardedHeaders()
            : app;
    }

    public static bool IsEnabled(IConfiguration configuration)
    {
        var value = configuration[$"{SectionName}:Enabled"];
        return bool.TryParse(value, out var enabled) && enabled;
    }

    private static IReadOnlyList<string> ReadValues(IConfiguration configuration, string key)
    {
        return configuration.GetSection($"{SectionName}:{key}")
            .GetChildren()
            .Select(child => child.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
            .ToArray();
    }

    private static IPAddress ParseIpAddress(string value, string key)
    {
        if (!IPAddress.TryParse(value, out var address))
        {
            throw new InvalidOperationException($"{key} contains invalid IP address '{value}'.");
        }

        return address;
    }

    private static System.Net.IPNetwork ParseNetwork(string value)
    {
        var parts = value.Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2
            || !IPAddress.TryParse(parts[0], out var prefix)
            || !int.TryParse(parts[1], out var prefixLength))
        {
            throw new InvalidOperationException($"Security:ForwardedHeaders:KnownNetworks contains invalid CIDR network '{value}'.");
        }

        return new System.Net.IPNetwork(prefix, prefixLength);
    }
}
