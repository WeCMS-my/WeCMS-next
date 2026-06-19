using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WeCms.Api.Security;

namespace WeCms.Tests.Unit.Api;

public sealed class ForwardedHeadersBehaviorTests
{
    [Fact]
    public async Task UseWeCmsForwardedHeaders_MapsRemoteIpFromXForwardedForForTrustedProxy()
    {
        var remoteIp = await SimulateForwardedHeadersAsync(
            new Dictionary<string, string?>
            {
                ["Security:ForwardedHeaders:Enabled"] = "true",
                ["Security:ForwardedHeaders:KnownProxies:0"] = "10.0.0.10",
                ["Security:ForwardedHeaders:ForwardLimit"] = "1"
            },
            "10.0.0.10",
            new Dictionary<string, string>
            {
                ["X-Forwarded-For"] = "198.51.100.44",
                ["X-Forwarded-Proto"] = "https"
            });

        Assert.Equal("198.51.100.44", remoteIp);
    }

    [Fact]
    public async Task UseWeCmsForwardedHeaders_DoesNotMapRemoteIpWhenDisabled()
    {
        var remoteIp = await SimulateForwardedHeadersAsync(
            new Dictionary<string, string?>
            {
                ["Security:ForwardedHeaders:Enabled"] = "false",
                ["Security:ForwardedHeaders:KnownProxies:0"] = "10.0.0.10",
                ["Security:ForwardedHeaders:ForwardLimit"] = "1"
            },
            "10.0.0.10",
            new Dictionary<string, string>
            {
                ["X-Forwarded-For"] = "198.51.100.44",
                ["X-Forwarded-Proto"] = "https"
            });

        Assert.Equal("10.0.0.10", remoteIp);
    }

    [Fact]
    public async Task UseWeCmsForwardedHeaders_DoesNotMapRemoteIpFromUnknownProxy()
    {
        var remoteIp = await SimulateForwardedHeadersAsync(
            new Dictionary<string, string?>
            {
                ["Security:ForwardedHeaders:Enabled"] = "true",
                ["Security:ForwardedHeaders:KnownProxies:0"] = "10.0.0.20",
                ["Security:ForwardedHeaders:KnownNetworks:0"] = "192.168.0.0/24",
                ["Security:ForwardedHeaders:ForwardLimit"] = "1"
            },
            "10.0.0.10",
            new Dictionary<string, string>
            {
                ["X-Forwarded-For"] = "198.51.100.44",
                ["X-Forwarded-Proto"] = "https"
            });

        Assert.Equal("10.0.0.10", remoteIp);
    }

    [Fact]
    public async Task UseWeCmsForwardedHeaders_MapsRemoteIpFromXForwardedForForTrustedNetwork()
    {
        var remoteIp = await SimulateForwardedHeadersAsync(
            new Dictionary<string, string?>
            {
                ["Security:ForwardedHeaders:Enabled"] = "true",
                ["Security:ForwardedHeaders:KnownNetworks:0"] = "10.0.0.0/24",
                ["Security:ForwardedHeaders:ForwardLimit"] = "1"
            },
            "10.0.0.10",
            new Dictionary<string, string>
            {
                ["X-Forwarded-For"] = "198.51.100.44",
                ["X-Forwarded-Proto"] = "https"
            });

        Assert.Equal("198.51.100.44", remoteIp);
    }

    private static async Task<string> SimulateForwardedHeadersAsync(
        IReadOnlyDictionary<string, string?> configuration,
        string remoteIp,
        IReadOnlyDictionary<string, string> headers)
    {
        var configurationBuilder = new ConfigurationBuilder()
            .AddInMemoryCollection(configuration);
        var configurationRoot = configurationBuilder.Build();

        var options = new Microsoft.AspNetCore.Builder.ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
            RequireHeaderSymmetry = true,
            ForwardLimit = ReadForwardLimit(configurationRoot),
        };

        foreach (var proxy in ReadValues(configurationRoot, "Security:ForwardedHeaders:KnownProxies"))
        {
            options.KnownProxies.Add(IPAddress.Parse(proxy));
        }

        foreach (var network in ReadValues(configurationRoot, "Security:ForwardedHeaders:KnownNetworks"))
        {
            options.KnownIPNetworks.Add(ParseIpNetwork(network));
        }

        var middleware = new ForwardedHeadersMiddleware(
            static _ => Task.CompletedTask,
            NullLoggerFactory.Instance,
            Options.Create(options));

        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(remoteIp);

        foreach (var (name, value) in headers)
        {
            context.Request.Headers[name] = value;
        }

        if (WeCmsForwardedHeadersExtensions.IsEnabled(configurationRoot))
        {
            await middleware.Invoke(context);
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "null";
    }

    private static int ReadForwardLimit(IConfiguration configuration)
    {
        const int defaultForwardLimit = 1;
        const int maxForwardLimit = 32;
        var value = configuration["Security:ForwardedHeaders:ForwardLimit"];
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultForwardLimit;
        }

        return int.TryParse(value, out var limit) && limit > 0 && limit <= maxForwardLimit
            ? limit
            : defaultForwardLimit;
    }

    private static IEnumerable<string> ReadValues(IConfiguration configuration, string key)
    {
        return configuration.GetSection(key)
            .GetChildren()
            .Select(child => child.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim());
    }

    private static System.Net.IPNetwork ParseIpNetwork(string value)
    {
        var parts = value.Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2
            || !IPAddress.TryParse(parts[0], out var prefix)
            || !int.TryParse(parts[1], out var prefixLength)
            || prefixLength < 0
            || (prefix.AddressFamily == AddressFamily.InterNetwork && prefixLength > 32)
            || (prefix.AddressFamily == AddressFamily.InterNetworkV6 && prefixLength > 128))
        {
            throw new InvalidOperationException($"Security:ForwardedHeaders:KnownNetworks contains invalid CIDR network '{value}'.");
        }

        return new System.Net.IPNetwork(prefix, prefixLength);
    }
}
