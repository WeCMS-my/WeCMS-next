using System.Text.RegularExpressions;

namespace WeCms.Tests.Unit.OpenApi;

public sealed partial class OpenApiExportTests
{
    private static HashSet<(string Path, string Method)> CollectSourceMappedEndpoints()
    {
        var mappedEndpoints = new HashSet<(string Path, string Method)>();

        foreach (var filePath in EnumerateEndpointSourceFiles())
        {
            mappedEndpoints.UnionWith(CollectSourceMappedEndpointsFromFile(filePath));
        }

        return mappedEndpoints;
    }

    private static string SourceRoot
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "backend", "WeCms.slnx")))
                {
                    return Path.Combine(directory.FullName, "backend", "src");
                }

                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException("Could not locate repository root.");
        }
    }

    private static HashSet<(string Path, string Method)> CollectSourceMappedEndpointsFromFile(string filePath)
    {
        var source = File.ReadAllText(filePath);
        var result = new HashSet<(string Path, string Method)>();

        var groupPrefix = Regex.Match(source, @"\b(?<groupName>\w+)\s*=\s*endpoints\.MapGroup\(""(?<prefix>[^""]+)""\)");
        var routePrefix = groupPrefix.Success
            ? groupPrefix.Groups["prefix"].Value
            : string.Empty;
        var groupName = groupPrefix.Success ? groupPrefix.Groups["groupName"].Value : null;

        const string endpointPattern =
            @"(?<receiver>\w+)\.Map(?<method>Get|Post|Put|Patch|Delete)\(\s*""(?<path>[^""\\]*)""\s*,";

        foreach (Match match in Regex.Matches(source, endpointPattern))
        {
            var method = match.Groups["method"].Value.ToLowerInvariant();
            var rawPath = match.Groups["path"].Value;
            var receiver = match.Groups["receiver"].Value;
            var path = receiver == groupName && !string.IsNullOrWhiteSpace(routePrefix)
                ? $"{routePrefix}{rawPath}"
                : rawPath;

            result.Add((path, method));
        }

        return result;
    }

    private static IEnumerable<string> EnumerateEndpointSourceFiles()
    {
        var accessControlEndpointFiles = Directory
            .EnumerateFiles(Path.Combine(SourceRoot, "WeCms.Modules.AccessControl"), "*.cs", SearchOption.AllDirectories)
            .Where(filePath =>
                filePath.EndsWith("Endpoints.cs", StringComparison.Ordinal)
                || filePath.EndsWith("EndpointExtensions.cs", StringComparison.Ordinal));
        var organizationEndpointFiles = Directory
            .EnumerateFiles(Path.Combine(SourceRoot, "WeCms.Modules.Organization"), "*.cs", SearchOption.AllDirectories)
            .Where(filePath => filePath.EndsWith("Endpoints.cs", StringComparison.Ordinal));
        var configurationEndpointFiles = Directory
            .EnumerateFiles(Path.Combine(SourceRoot, "WeCms.Modules.Configuration"), "*.cs", SearchOption.AllDirectories)
            .Where(filePath => filePath.EndsWith("Endpoints.cs", StringComparison.Ordinal));
        var auditEndpointFiles = Directory
            .EnumerateFiles(Path.Combine(SourceRoot, "WeCms.Modules.Audit"), "*.cs", SearchOption.AllDirectories)
            .Where(filePath => filePath.EndsWith("Endpoints.cs", StringComparison.Ordinal));
        var securityEndpointFiles = Directory
            .EnumerateFiles(Path.Combine(SourceRoot, "WeCms.Modules.Security"), "*.cs", SearchOption.AllDirectories)
            .Where(filePath => filePath.EndsWith("Endpoints.cs", StringComparison.Ordinal));
        var fileCenterEndpointFiles = Directory
            .EnumerateFiles(Path.Combine(SourceRoot, "WeCms.Modules.FileCenter"), "*.cs", SearchOption.AllDirectories)
            .Where(filePath => filePath.EndsWith("Endpoints.cs", StringComparison.Ordinal));
        var platformEndpointFiles = Directory
            .EnumerateFiles(Path.Combine(SourceRoot, "WeCms.Modules.Platform"), "*.cs", SearchOption.AllDirectories)
            .Where(filePath => filePath.EndsWith("Endpoints.cs", StringComparison.Ordinal)
                || filePath.EndsWith("EndpointExtensions.cs", StringComparison.Ordinal));
        var endpointDefinitionFiles = Directory
            .EnumerateFiles(Path.Combine(SourceRoot, "WeCms.Api", "Endpoints"), "*EndpointDefinition.cs", SearchOption.TopDirectoryOnly);
        var identityEndpointDefinitionFiles = Directory
            .EnumerateFiles(Path.Combine(SourceRoot, "WeCms.Modules.Identity", "Endpoints"), "*EndpointDefinition.cs", SearchOption.TopDirectoryOnly);

        return accessControlEndpointFiles
            .Concat(organizationEndpointFiles)
            .Concat(configurationEndpointFiles)
            .Concat(auditEndpointFiles)
            .Concat(securityEndpointFiles)
            .Concat(fileCenterEndpointFiles)
            .Concat(platformEndpointFiles)
            .Concat(endpointDefinitionFiles)
            .Concat(identityEndpointDefinitionFiles)
            .OrderBy(filePath => filePath, StringComparer.Ordinal);
    }
}
