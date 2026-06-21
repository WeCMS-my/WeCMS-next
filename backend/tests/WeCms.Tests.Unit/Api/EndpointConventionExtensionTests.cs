using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using WeCms.Api.Endpoints;
using WeCms.Shared;
using WeCms.Shared.Endpoints;

namespace WeCms.Tests.Unit.Api;

public sealed class EndpointConventionExtensionTests
{
    [Fact]
    public void EndpointConventionExtensions_CanBeComposedAndAppendMetadata()
    {
        var conventions = new TestEndpointConventionBuilder();
        var builder = new RouteHandlerBuilder([conventions]);

        builder
            .WithModule("identity")
            .ProducesApi<SampleResponse>()
            .Audit("identity", "users", "create")
            .RequirePermission("sys:user:create")
            .RequireButtonPermission("sys:user:create-button")
            .RequireUrlPermission("sys:user:create-url")
            .Validate<SampleRequest>();

        var endpointBuilder = CreateEndpointBuilder();
        endpointBuilder.Metadata.Add(new EndpointModuleMetadata("existing"));
        conventions.Apply(endpointBuilder);

        Assert.Equal(["existing", "identity"], endpointBuilder.Metadata.OfType<EndpointModuleMetadata>().Select(static metadata => metadata.Module));
        Assert.Equal(typeof(SampleResponse), endpointBuilder.Metadata.OfType<OpenApiResponseMetadata>().Single().ResponseType);
        var audit = endpointBuilder.Metadata.OfType<EndpointAuditMetadata>().Single();
        Assert.Equal("identity", audit.Module);
        Assert.Equal("users", audit.Resource);
        Assert.Equal("create", audit.Action);
        Assert.Equal(
            [EndpointPermissionKind.Api, EndpointPermissionKind.Button, EndpointPermissionKind.Url],
            endpointBuilder.Metadata.OfType<EndpointPermissionMetadata>().Select(static metadata => metadata.Kind));
        Assert.Equal(typeof(SampleRequest), endpointBuilder.Metadata.OfType<EndpointValidationMetadata>().Single().RequestType);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void EndpointConventionExtensions_FailFastForInvalidTextArguments(string value)
    {
        var builder = new RouteHandlerBuilder([new TestEndpointConventionBuilder()]);

        Assert.Throws<ArgumentException>(() => builder.WithModule(value));
        Assert.Throws<ArgumentException>(() => builder.Audit(value, "users", "create"));
        Assert.Throws<ArgumentException>(() => builder.Audit("identity", value, "create"));
        Assert.Throws<ArgumentException>(() => builder.Audit("identity", "users", value));
        Assert.Throws<ArgumentException>(() => builder.RequirePermission(value));
        Assert.Throws<ArgumentException>(() => builder.RequireButtonPermission(value));
        Assert.Throws<ArgumentException>(() => builder.RequireUrlPermission(value));
    }

    private static RouteEndpointBuilder CreateEndpointBuilder()
    {
        return new RouteEndpointBuilder(
            static _ => Task.CompletedTask,
            RoutePatternFactory.Parse("/metadata"),
            order: 0);
    }

    private sealed record SampleRequest(string Name);

    private sealed record SampleResponse(long Id);

    private sealed class TestEndpointConventionBuilder : IEndpointConventionBuilder
    {
        private readonly List<Action<EndpointBuilder>> conventions = [];

        public void Add(Action<EndpointBuilder> convention)
        {
            conventions.Add(convention);
        }

        public void Apply(EndpointBuilder builder)
        {
            foreach (var convention in conventions)
            {
                convention(builder);
            }
        }
    }
}
