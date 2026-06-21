using WeCms.Shared.Endpoints;

namespace WeCms.Api.Endpoints;

public sealed class EndpointDefinitionRegistry
{
    private readonly List<IEndpointDefinition> definitions = [];

    public IReadOnlyList<IEndpointDefinition> Definitions => definitions;

    public EndpointDefinitionRegistry Add(IEndpointDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        definitions.Add(definition);
        return this;
    }
}
