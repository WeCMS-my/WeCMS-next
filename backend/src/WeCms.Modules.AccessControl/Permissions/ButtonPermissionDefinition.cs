using System.Text.RegularExpressions;

namespace WeCms.Modules.AccessControl.Permissions;

public sealed partial record ButtonPermissionDefinition
{
    public ButtonPermissionDefinition(
        string buttonKey,
        string menuCode,
        string permissionCode,
        string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (string.IsNullOrWhiteSpace(buttonKey) || !ButtonKeyRegex().IsMatch(buttonKey))
        {
            throw new ArgumentException("Button key is invalid.", nameof(buttonKey));
        }

        if (string.IsNullOrWhiteSpace(menuCode) || !MenuCodeRegex().IsMatch(menuCode))
        {
            throw new ArgumentException("Menu code is invalid.", nameof(menuCode));
        }

        var normalizedPermissionCode = permissionCode ?? string.Empty;
        var match = ButtonPermissionCodeRegex().Match(normalizedPermissionCode);
        if (!match.Success)
        {
            throw new ArgumentException("Button permission code must use domain:resource:button:action format.", nameof(permissionCode));
        }

        var action = match.Groups["action"].Value;
        if (!StringComparer.Ordinal.Equals(action, buttonKey))
        {
            throw new ArgumentException("Button permission action must match the button key.", nameof(permissionCode));
        }

        ButtonKey = buttonKey;
        MenuCode = menuCode;
        PermissionCode = normalizedPermissionCode;
        Module = match.Groups["module"].Value;
        Resource = match.Groups["resource"].Value;
        Name = name;
    }

    public string ButtonKey { get; }

    public string MenuCode { get; }

    public string PermissionCode { get; }

    public string Module { get; }

    public string Resource { get; }

    public string Name { get; }

    [GeneratedRegex("^[a-z][a-z0-9]*(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex ButtonKeyRegex();

    [GeneratedRegex("^[a-z][A-Za-z0-9]*(?:\\.[a-z][A-Za-z0-9]*)+$", RegexOptions.CultureInvariant)]
    private static partial Regex MenuCodeRegex();

    [GeneratedRegex("^(?<module>[a-z][a-z0-9]*):(?<resource>[a-z][a-z0-9]*(?:-[a-z0-9]+)*):button:(?<action>[a-z][a-z0-9]*(?:-[a-z0-9]+)*)$", RegexOptions.CultureInvariant)]
    private static partial Regex ButtonPermissionCodeRegex();
}
