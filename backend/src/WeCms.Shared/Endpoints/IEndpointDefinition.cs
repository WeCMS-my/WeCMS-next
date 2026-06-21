using Microsoft.AspNetCore.Routing;

namespace WeCms.Shared.Endpoints;

public interface IEndpointDefinition
{
    void MapEndpoints(IEndpointRouteBuilder endpoints);
}
