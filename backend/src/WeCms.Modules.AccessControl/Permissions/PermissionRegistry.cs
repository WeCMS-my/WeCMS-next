namespace WeCms.Modules.AccessControl.Permissions;

public sealed class PermissionRegistry
{
    private readonly IReadOnlyDictionary<string, PermissionDefinition> _definitionsByCode;

    private PermissionRegistry(
        IReadOnlyList<PermissionGroupDefinition> groups,
        IReadOnlyDictionary<string, PermissionDefinition> definitionsByCode)
    {
        Groups = groups;
        Definitions = definitionsByCode.Values.OrderBy(static definition => definition.Code, StringComparer.Ordinal).ToArray();
        _definitionsByCode = definitionsByCode;
    }

    public IReadOnlyList<PermissionGroupDefinition> Groups { get; }

    public IReadOnlyList<PermissionDefinition> Definitions { get; }

    public static PermissionRegistry FromProviders(IEnumerable<PermissionDefinitionProvider> providers)
    {
        ArgumentNullException.ThrowIfNull(providers);

        var groups = new List<PermissionGroupDefinition>();
        var definitionsByCode = new Dictionary<string, PermissionDefinition>(StringComparer.Ordinal);

        foreach (var provider in providers)
        {
            ArgumentNullException.ThrowIfNull(provider);

            foreach (var group in provider.GetGroups())
            {
                groups.Add(group);
                foreach (var definition in group.Definitions)
                {
                    if (!definitionsByCode.TryAdd(definition.Code, definition))
                    {
                        throw new InvalidOperationException($"Duplicate permission code: {definition.Code}");
                    }
                }
            }
        }

        return new PermissionRegistry(groups, definitionsByCode);
    }

    public PermissionDefinition GetRequired(string code)
    {
        if (_definitionsByCode.TryGetValue(code, out var definition))
        {
            return definition;
        }

        throw new KeyNotFoundException($"Permission definition was not found: {code}");
    }
}
