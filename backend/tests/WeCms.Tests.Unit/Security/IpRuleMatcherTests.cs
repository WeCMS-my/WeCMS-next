using System.Net;
using WeCms.Shared.Security;

namespace WeCms.Tests.Unit.Security;

public sealed class IpRuleMatcherTests
{
    private readonly IIpRuleMatcher _matcher = new IpRuleMatcher();

    [Fact]
    public void IsMatch_MatchesIpv4ExactRule()
    {
        Assert.True(_matcher.IsMatch("192.168.10.12", IPAddress.Parse("192.168.10.12")));
        Assert.False(_matcher.IsMatch("192.168.10.12", IPAddress.Parse("192.168.10.13")));
    }

    [Fact]
    public void IsMatch_MatchesIpv4CidrRule()
    {
        Assert.True(_matcher.IsMatch("192.168.10.0/24", IPAddress.Parse("192.168.10.200")));
        Assert.False(_matcher.IsMatch("192.168.10.0/24", IPAddress.Parse("192.168.11.1")));
    }

    [Fact]
    public void IsMatch_MatchesIpv6ExactRule()
    {
        Assert.True(_matcher.IsMatch("2001:db8::1", IPAddress.Parse("2001:db8::1")));
        Assert.False(_matcher.IsMatch("2001:db8::1", IPAddress.Parse("2001:db8::2")));
    }

    [Fact]
    public void IsMatch_MatchesIpv6CidrRule()
    {
        Assert.True(_matcher.IsMatch("2001:db8:abcd::/48", IPAddress.Parse("2001:db8:abcd:0:0:0:0:42")));
        Assert.False(_matcher.IsMatch("2001:db8:abcd::/48", IPAddress.Parse("2001:db8:abce::1")));
    }

    [Fact]
    public void IsMatch_ParsesCommaNewlineAndWhitespaceSeparatedRules()
    {
        const string rules = "10.10.0.1, 10.20.0.0/16\n2001:db8::1  203.0.113.7";

        Assert.True(_matcher.IsMatch(rules, IPAddress.Parse("10.20.8.9")));
        Assert.True(_matcher.IsMatch(rules, IPAddress.Parse("2001:db8::1")));
        Assert.True(_matcher.IsMatch(rules, IPAddress.Parse("203.0.113.7")));
    }

    [Fact]
    public void IsMatch_EmptyRulesDoNotMatch()
    {
        Assert.False(_matcher.IsMatch(" \n\t ", IPAddress.Parse("192.168.10.12")));
    }

    [Theory]
    [InlineData("not-an-ip")]
    [InlineData("192.168.1.0/33")]
    [InlineData("2001:db8::/129")]
    [InlineData("192.168.1.0/not-number")]
    public void IsMatch_InvalidRulesFailFast(string rule)
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => _matcher.IsMatch(rule, IPAddress.Parse("192.168.10.12")));

        Assert.Equal($"Invalid IP rule: {rule}.", exception.Message);
    }
}
