namespace WeCms.Shared.Security;

public interface ISecurityEventClassifier
{
    SecurityEventClassification Classify(string eventType, string? traceId);
}

public sealed record SecurityEventRule(string EventType, string Severity, string Source);

public sealed record SecurityEventClassification(string EventType, string Severity, string Source, string TraceId);

public sealed class SecurityEventClassifier : ISecurityEventClassifier
{
    private static readonly IReadOnlyDictionary<string, SecurityEventRule> Rules = new Dictionary<string, SecurityEventRule>(StringComparer.OrdinalIgnoreCase)
    {
        ["login_failure"] = new("login_failure", "warning", "auth"),
        ["login_bruteforce"] = new("login_bruteforce", "critical", "auth"),
        ["csrf_origin_rejected"] = new("csrf_origin_rejected", "warning", "csrf"),
        ["ip_blocked"] = new("ip_blocked", "critical", "ip-access"),
        ["security_ban_hit"] = new("security_ban_hit", "critical", "security-ban"),
        ["permission_denied"] = new("permission_denied", "warning", "permission"),
        ["suspicious_payload"] = new("suspicious_payload", "warning", "request"),
        ["file_upload_rejected"] = new("file_upload_rejected", "warning", "file"),
        ["two_factor_failed"] = new("two_factor_failed", "warning", "two-factor"),
        ["two_factor_replay"] = new("two_factor_replay", "critical", "two-factor"),
        ["settings_sensitive_changed"] = new("settings_sensitive_changed", "warning", "settings"),
        ["role_permission_changed"] = new("role_permission_changed", "warning", "permission"),
        ["rate_limit_hit"] = new("rate_limit_hit", "warning", "rate-limit"),
        ["auth.login_failed"] = new("login_failure", "warning", "auth"),
        ["auth.login_ban_threshold_reached"] = new("login_bruteforce", "critical", "auth"),
        ["auth.csrf_origin_rejected"] = new("csrf_origin_rejected", "warning", "csrf"),
        ["security.ip_rejected"] = new("ip_blocked", "critical", "ip-access"),
        ["security.ban_hit"] = new("security_ban_hit", "critical", "security-ban"),
        ["auth.2fa_failed"] = new("two_factor_failed", "warning", "two-factor"),
        ["auth.two_factor_failed"] = new("two_factor_failed", "warning", "two-factor"),
        ["auth.2fa_replay"] = new("two_factor_replay", "critical", "two-factor"),
        ["security.setting_changed"] = new("settings_sensitive_changed", "warning", "settings"),
        ["security.role_permission_changed"] = new("role_permission_changed", "warning", "permission"),
        ["security.rate_limited"] = new("rate_limit_hit", "warning", "rate-limit")
    };

    public SecurityEventClassification Classify(string eventType, string? traceId)
    {
        var normalizedEventType = string.IsNullOrWhiteSpace(eventType) ? "unknown" : eventType.Trim();
        var rule = Rules.GetValueOrDefault(normalizedEventType) ?? InferRule(normalizedEventType);
        return new SecurityEventClassification(rule.EventType, rule.Severity, rule.Source, NormalizeTraceId(traceId));
    }

    private static SecurityEventRule InferRule(string eventType)
    {
        if (eventType.Contains("2fa", StringComparison.OrdinalIgnoreCase) || eventType.Contains("two_factor", StringComparison.OrdinalIgnoreCase))
        {
            return new SecurityEventRule(eventType, "warning", "two-factor");
        }

        if (eventType.Contains("ban", StringComparison.OrdinalIgnoreCase))
        {
            return new SecurityEventRule(eventType, "critical", "security-ban");
        }

        if (eventType.Contains("setting", StringComparison.OrdinalIgnoreCase))
        {
            return new SecurityEventRule(eventType, "warning", "settings");
        }

        if (eventType.Contains("permission", StringComparison.OrdinalIgnoreCase))
        {
            return new SecurityEventRule(eventType, "warning", "permission");
        }

        if (eventType.Contains("rate_limit", StringComparison.OrdinalIgnoreCase))
        {
            return new SecurityEventRule(eventType, "warning", "rate-limit");
        }

        if (eventType.Contains("login", StringComparison.OrdinalIgnoreCase) || eventType.Contains("auth", StringComparison.OrdinalIgnoreCase))
        {
            return new SecurityEventRule(eventType, "warning", "auth");
        }

        return new SecurityEventRule(eventType, "info", "system");
    }

    private static string NormalizeTraceId(string? traceId)
    {
        if (string.IsNullOrWhiteSpace(traceId))
        {
            return "unknown";
        }

        var normalized = traceId.Trim();
        return normalized.Length <= 64 ? normalized : normalized[..64];
    }
}
