using System.Net;
using System.Net.Sockets;

namespace WeCms.Shared.Security;

public readonly record struct ParsedIpRule(IPAddress Network, int PrefixLength);

public interface IIpRuleMatcher
{
    bool IsMatch(string rules, IPAddress address);

    IReadOnlyList<ParsedIpRule> ParseRules(string[] allowedRules);

    bool IsMatch(IReadOnlyList<ParsedIpRule> rules, IPAddress address);
}

public sealed class IpRuleMatcher : IIpRuleMatcher
{
    public bool IsMatch(string rules, IPAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);

        return IsMatch(ParseRules(SplitRules(rules).ToArray()), address);
    }

    public IReadOnlyList<ParsedIpRule> ParseRules(string[] allowedRules)
    {
        var parsedRules = new List<ParsedIpRule>();
        foreach (var ruleText in allowedRules)
        {
            if (string.IsNullOrWhiteSpace(ruleText))
            {
                continue;
            }

            parsedRules.Add(ParseRule(ruleText.Trim()));
        }

        return parsedRules;
    }

    public bool IsMatch(IReadOnlyList<ParsedIpRule> rules, IPAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);
        if (rules is null || rules.Count == 0)
        {
            return false;
        }

        address = NormalizeAddress(address);
        foreach (var rule in rules)
        {
            if (IsRuleMatch(rule, address))
            {
                return true;
            }
        }

        return false;
    }

    public static IReadOnlyList<string> SplitRules(string rules)
    {
        if (string.IsNullOrWhiteSpace(rules))
        {
            return [];
        }

        return rules.Split([',', '\r', '\n', '\t', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static bool IsRuleMatch(ParsedIpRule rule, IPAddress address)
    {
        if (rule.Network.AddressFamily != address.AddressFamily)
        {
            return false;
        }

        if (rule.PrefixLength == GetAddressBitLength(address.AddressFamily))
        {
            return rule.Network.Equals(address);
        }

        var networkBytes = rule.Network.GetAddressBytes();
        var addressBytes = address.GetAddressBytes();
        var fullBytes = rule.PrefixLength / 8;
        var remainingBits = rule.PrefixLength % 8;

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

    private static ParsedIpRule ParseRule(string rule)
    {
        var slashIndex = rule.IndexOf('/', StringComparison.Ordinal);
        if (slashIndex < 0)
        {
            if (!TryParseAddress(rule, out var exactAddress))
            {
                throw InvalidRule(rule);
            }

            exactAddress = NormalizeAddress(exactAddress);
            return new ParsedIpRule(exactAddress, GetAddressBitLength(exactAddress.AddressFamily));
        }

        var addressText = rule[..slashIndex];
        var prefixText = rule[(slashIndex + 1)..];
        if (!TryParseAddress(addressText, out var networkAddress)
            || !int.TryParse(prefixText, global::System.Globalization.NumberStyles.None, global::System.Globalization.CultureInfo.InvariantCulture, out var prefixLength))
        {
            throw InvalidRule(rule);
        }

        networkAddress = NormalizeAddress(networkAddress);
        var maxPrefixLength = GetAddressBitLength(networkAddress.AddressFamily);
        if (prefixLength < 0 || prefixLength > maxPrefixLength)
        {
            throw InvalidRule(rule);
        }

        return new ParsedIpRule(networkAddress, prefixLength);
    }

    private static int GetAddressBitLength(AddressFamily addressFamily)
    {
        return addressFamily == AddressFamily.InterNetwork ? 32 : 128;
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
