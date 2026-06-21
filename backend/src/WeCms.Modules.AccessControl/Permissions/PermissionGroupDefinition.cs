namespace WeCms.Modules.AccessControl.Permissions;

public sealed record PermissionGroupDefinition
{
    public PermissionGroupDefinition(string module, IReadOnlyList<PermissionDefinition> definitions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(module);
        ArgumentNullException.ThrowIfNull(definitions);

        foreach (var definition in definitions)
        {
            if (!StringComparer.Ordinal.Equals(definition.Module, module))
            {
                throw new ArgumentException("Permission definition module must match the group module.", nameof(definitions));
            }
        }

        Module = module;
        Definitions = definitions;
    }

    public string Module { get; }

    public IReadOnlyList<PermissionDefinition> Definitions { get; }
}
