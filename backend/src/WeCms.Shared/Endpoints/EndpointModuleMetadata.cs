namespace WeCms.Shared.Endpoints;

public sealed record EndpointModuleMetadata(string Module);

public static class EndpointOpenApiExtensionNames
{
    public const string Module = "x-wecms-module";
    public const string Permission = "x-wecms-permission";
    public const string Audit = "x-wecms-audit";
    public const string RateLimit = "x-wecms-rate-limit";
}
