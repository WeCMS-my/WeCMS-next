using System.Net;
using System.Net.Sockets;

namespace WeCms.Shared.Security;

public interface IIpRuleMatcher
{
    bool IsMatch(string rules, IPAddress address);
}

public sealed class IpRuleMatcher : IIpRuleMatcher
{
    public bool IsMatch(string rules, IPAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);

        foreach (var rule in SplitRules(rules))
        {
            if (IsRuleMatch(rule, NormalizeAddress(address)))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsRuleMatch(string rule, IPAddress address)
    {
        var slashIndex = rule.IndexOf('/', StringComparison.Ordinal);
        if (slashIndex < 0)
        {
            return TryParseAddress(rule, out var exactAddress)
                ? NormalizeAddress(exactAddress).Equals(address)
                : throw InvalidRule(rule);
        }

        var addressText = rule[..slashIndex];
        var prefixText = rule[(slashIndex + 1)..];
        if (!TryParseAddress(addressText, out var networkAddress)
            || !int.TryParse(prefixText, global::System.Globalization.NumberStyles.None, global::System.Globalization.CultureInfo.InvariantCulture, out var prefixLength))
        {
            throw InvalidRule(rule);
        }

        networkAddress = NormalizeAddress(networkAddress);
        var maxPrefixLength = networkAddress.AddressFamily == AddressFamily.InterNetwork ? 32 : 128;
        if (prefixLength < 0 || prefixLength > maxPrefixLength)
        {
            throw InvalidRule(rule);
        }

        if (networkAddress.AddressFamily != address.AddressFamily)
        {
            return false;
        }

        return IsCidrMatch(networkAddress.GetAddressBytes(), address.GetAddressBytes(), prefixLength);
    }

    private static bool IsCidrMatch(byte[] networkBytes, byte[] addressBytes, int prefixLength)
    {
        var fullBytes = prefixLength / 8;
        var remainingBits = prefixLength % 8;

        for (var index = 0; index < fullBytes; index++)
        {
            if (networkBytes[index] != addressBytes[index])
            {
                return false;
            }
        }

        if (remainingBits == 0)
        {
            return true;
        }

        var mask = (byte)(0xFF << (8 - remainingBits));
        return (networkBytes[fullBytes] & mask) == (addressBytes[fullBytes] & mask);
    }

    private static IEnumerable<string> SplitRules(string rules)
    {
        if (string.IsNullOrWhiteSpace(rules))
        {
            yield break;
        }

        foreach (var rule in rules.Split([',', '\r', '\n', '\t', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            yield return rule;
        }
    }

    private static bool TryParseAddress(string value, out IPAddress address)
    {
        if (IPAddress.TryParse(value, out var parsed))
        {
            address = parsed;
            return true;
        }

        address = IPAddress.None;
        return false;
    }

    private static IPAddress NormalizeAddress(IPAddress address)
    {
        return address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;
    }

    private static InvalidOperationException InvalidRule(string rule)
    {
        return new InvalidOperationException($"Invalid IP rule: {rule}.");
    }
}
