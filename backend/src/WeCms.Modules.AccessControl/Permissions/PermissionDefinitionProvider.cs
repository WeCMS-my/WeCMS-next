namespace WeCms.Modules.AccessControl.Permissions;

public abstract class PermissionDefinitionProvider
{
    public abstract IReadOnlyList<PermissionGroupDefinition> GetGroups();
}
