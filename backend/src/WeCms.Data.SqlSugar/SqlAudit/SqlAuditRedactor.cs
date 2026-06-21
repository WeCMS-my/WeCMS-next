using System.Collections.ObjectModel;
using System.Globalization;
using SqlSugar;

namespace WeCms.Data.SqlSugar;

public sealed class SqlAuditRedactor
{
    public const string RedactedValue = "***REDACTED***";

    private static readonly string[] SensitiveNames =
    [
        "password",
        "password_hash",
        "token",
        "refresh_token",
        "access_token",
        "secret",
        "two_factor",
        "recovery_code",
        "private_key",
        "connection_string"
    ];

    public IReadOnlyDictionary<string, string?> Redact(IReadOnlyList<SugarParameter> parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        var redacted = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var parameter in parameters)
        {
            var name = string.IsNullOrWhiteSpace(parameter.ParameterName)
                ? "(unnamed)"
                : parameter.ParameterName.Trim();
            redacted[name] = IsSensitiveName(name)
                ? RedactedValue
                : ToAuditValue(parameter.Value);
        }

        return new ReadOnlyDictionary<string, string?>(redacted);
    }

    private static bool IsSensitiveName(string name)
    {
        var normalized = NormalizeName(name);
        return SensitiveNames.Any(sensitive =>
            normalized.Equals(sensitive, StringComparison.OrdinalIgnoreCase)
            || normalized.Contains(sensitive, StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeName(string name)
    {
        return name
            .Trim()
            .TrimStart('@', ':', '?')
            .Replace("-", "_", StringComparison.Ordinal)
            .ToLowerInvariant();
    }

    private static string? ToAuditValue(object? value)
    {
        return value switch
        {
            null => null,
            DBNull => null,
            DateTime dateTime => dateTime.ToString("O", CultureInfo.InvariantCulture),
            DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O", CultureInfo.InvariantCulture),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString()
        };
    }
}
