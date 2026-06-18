using WeCms.Shared.Security;

namespace WeCms.Tests.Unit.Security;

public sealed class SecurityEventClassifierTests
{
    [Theory]
    [InlineData("login_failure", "warning", "auth")]
    [InlineData("login_bruteforce", "critical", "auth")]
    [InlineData("csrf_origin_rejected", "warning", "csrf")]
    [InlineData("ip_blocked", "critical", "ip-access")]
    [InlineData("security_ban_hit", "critical", "security-ban")]
    [InlineData("permission_denied", "warning", "permission")]
    [InlineData("suspicious_payload", "warning", "request")]
    [InlineData("file_upload_rejected", "warning", "file")]
    [InlineData("two_factor_failed", "warning", "two-factor")]
    [InlineData("two_factor_replay", "critical", "two-factor")]
    [InlineData("settings_sensitive_changed", "warning", "settings")]
    [InlineData("role_permission_changed", "warning", "permission")]
    public void Classify_KnownEvents_ReturnsSeveritySourceAndTraceId(string eventType, string severity, string source)
    {
        var classifier = new SecurityEventClassifier();

        var classification = classifier.Classify(eventType, "trace-123");

        Assert.Equal(eventType, classification.EventType);
        Assert.Equal(severity, classification.Severity);
        Assert.Equal(source, classification.Source);
        Assert.Equal("trace-123", classification.TraceId);
    }

    [Fact]
    public void Classify_UnknownEvent_ReturnsDefaultClassification()
    {
        var classifier = new SecurityEventClassifier();

        var classification = classifier.Classify("custom.event", null);

        Assert.Equal("custom.event", classification.EventType);
        Assert.Equal("info", classification.Severity);
        Assert.Equal("system", classification.Source);
        Assert.Equal("unknown", classification.TraceId);
    }

    [Fact]
    public void Classify_LegacyDotEvent_NormalizesToCanonicalEventType()
    {
        var classifier = new SecurityEventClassifier();

        var classification = classifier.Classify("security.setting_changed", "trace-settings");

        Assert.Equal("settings_sensitive_changed", classification.EventType);
        Assert.Equal("warning", classification.Severity);
        Assert.Equal("settings", classification.Source);
        Assert.Equal("trace-settings", classification.TraceId);
    }

    [Theory]
    [InlineData("auth.two_factor_failed", "two_factor_failed", "warning")]
    [InlineData("auth.2fa_replay", "two_factor_replay", "critical")]
    public void Classify_TwoFactorAliases_NormalizesToCanonicalEventType(string eventType, string expectedEventType, string expectedSeverity)
    {
        var classifier = new SecurityEventClassifier();

        var classification = classifier.Classify(eventType, "trace-2fa");

        Assert.Equal(expectedEventType, classification.EventType);
        Assert.Equal(expectedSeverity, classification.Severity);
        Assert.Equal("two-factor", classification.Source);
        Assert.Equal("trace-2fa", classification.TraceId);
    }
}
