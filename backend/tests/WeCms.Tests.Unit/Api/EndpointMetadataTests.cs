using Microsoft.AspNetCore.Http;
using WeCms.Shared.Endpoints;

namespace WeCms.Tests.Unit.Api;

public sealed class EndpointMetadataTests
{
    [Fact]
    public void EndpointMetadataCollection_ExposesWeCmsMetadata()
    {
        var endpoint = new Endpoint(
            static _ => Task.CompletedTask,
            new EndpointMetadataCollection(
                new EndpointModuleMetadata("identity"),
                new EndpointAuditMetadata("identity", "users", "create"),
                new EndpointPermissionMetadata("sys:user:create", EndpointPermissionKind.Url),
                new EndpointRateLimitMetadata("admin-write")),
            "metadata-test");

        var module = endpoint.Metadata.GetMetadata<EndpointModuleMetadata>();
        var audit = endpoint.Metadata.GetMetadata<EndpointAuditMetadata>();
        var permission = endpoint.Metadata.GetMetadata<EndpointPermissionMetadata>();
        var rateLimit = endpoint.Metadata.GetMetadata<EndpointRateLimitMetadata>();

        Assert.NotNull(module);
        Assert.Equal("identity", module.Module);
        Assert.NotNull(audit);
        Assert.Equal("identity", audit.Module);
        Assert.Equal("users", audit.Resource);
        Assert.Equal("create", audit.Action);
        Assert.NotNull(permission);
        Assert.Equal("sys:user:create", permission.PermissionCode);
        Assert.Equal(EndpointPermissionKind.Url, permission.Kind);
        Assert.NotNull(rateLimit);
        Assert.Equal("admin-write", rateLimit.PolicyName);
    }

    [Fact]
    public void EndpointOpenApiExtensionNames_DefinesWeCmsExtensionFields()
    {
        Assert.Equal("x-wecms-module", EndpointOpenApiExtensionNames.Module);
        Assert.Equal("x-wecms-permission", EndpointOpenApiExtensionNames.Permission);
        Assert.Equal("x-wecms-audit", EndpointOpenApiExtensionNames.Audit);
        Assert.Equal("x-wecms-rate-limit", EndpointOpenApiExtensionNames.RateLimit);
    }
}
