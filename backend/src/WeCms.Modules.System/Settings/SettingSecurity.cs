using Microsoft.AspNetCore.DataProtection;

namespace WeCms.Modules.System.Settings;

public interface ISettingDefinitionProvider
{
    SettingDefinition? Find(string key);
}

public interface ISettingSecretProtector
{
    string Protect(string value);
    string Unprotect(string protectedValue);
}

public interface ISettingCache
{
    Task RefreshAsync(CancellationToken cancellationToken);
}

public sealed class SettingDefinitionProvider : ISettingDefinitionProvider
{
    private static readonly IReadOnlyDictionary<string, SettingDefinition> Definitions = new Dictionary<string, SettingDefinition>(StringComparer.Ordinal)
    {
        ["security.passwordPepper"] = new("security.passwordPepper", true, false, true, "string"),
        ["security.ipAllowRules"] = new("security.ipAllowRules", false, false, true, "string"),
        ["security.ipDenyRules"] = new("security.ipDenyRules", false, false, true, "string"),
        ["smtp_pass"] = new("smtp_pass", true, false, false, "string"),
        ["auth_key"] = new("auth_key", true, true, true, "string"),
        ["jwt_secret"] = new("jwt_secret", true, false, true, "string"),
        ["storage_secret"] = new("storage_secret", true, false, true, "string")
    };

    public SettingDefinition? Find(string key)
    {
        return Definitions.GetValueOrDefault(key);
    }
}

public sealed class DataProtectionSettingSecretProtector : ISettingSecretProtector
{
    private const string Prefix = "dp:";
    private readonly IDataProtector _protector;

    public DataProtectionSettingSecretProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("WeCms.Settings.Secret.v1");
    }

    public string Protect(string value)
    {
        return Prefix + _protector.Protect(value);
    }

    public string Unprotect(string protectedValue)
    {
        return protectedValue.StartsWith(Prefix, StringComparison.Ordinal)
            ? _protector.Unprotect(protectedValue[Prefix.Length..])
            : protectedValue;
    }
}

public sealed class SettingCache : ISettingCache
{
    public Task RefreshAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }
}
