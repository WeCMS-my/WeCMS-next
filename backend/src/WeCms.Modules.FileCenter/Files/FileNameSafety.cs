using WeCms.Shared;

namespace WeCms.Modules.FileCenter.Files;

public static class FileNameSafety
{
    private static readonly HashSet<string> ReservedDeviceNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON",
        "PRN",
        "AUX",
        "NUL",
        "COM1",
        "COM2",
        "COM3",
        "COM4",
        "COM5",
        "COM6",
        "COM7",
        "COM8",
        "COM9",
        "LPT1",
        "LPT2",
        "LPT3",
        "LPT4",
        "LPT5",
        "LPT6",
        "LPT7",
        "LPT8",
        "LPT9"
    };

    public static string NormalizeFileName(string? value, string name, int maxLength)
    {
        var normalized = NormalizeRequired(value, name, maxLength);
        EnsureSafeFileName(normalized, name);
        return normalized;
    }

    public static string NormalizeFileExtension(string fileName)
    {
        return NormalizeStoredFileExtension(Path.GetExtension(fileName));
    }

    public static string NormalizeStoredFileExtension(string? fileExt)
    {
        if (string.IsNullOrWhiteSpace(fileExt))
        {
            throw Validation("file extension is required.");
        }

        var normalized = fileExt.ToLowerInvariant();
        if (normalized.Length is < 2 or > 16 || normalized[0] != '.' || normalized.Skip(1).Any(ch => !char.IsAsciiLetterOrDigit(ch)))
        {
            throw Validation("file extension contains invalid characters.");
        }

        return normalized;
    }

    private static string NormalizeRequired(string? value, string name, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw Validation($"{name} is required.");
        }

        if (!string.Equals(value, value.Trim(), StringComparison.Ordinal))
        {
            throw Validation($"{name} contains invalid characters.");
        }

        return value.Length <= maxLength ? value : throw Validation($"value must be {maxLength} characters or fewer.");
    }

    private static void EnsureSafeFileName(string value, string name)
    {
        if (value is "." or ".."
            || value.StartsWith(".", StringComparison.Ordinal)
            || value.EndsWith(".", StringComparison.Ordinal)
            || value.EndsWith(" ", StringComparison.Ordinal)
            || value.Any(ch => char.IsControl(ch) || ch is '"' or '<' or '>' or ':' or '/' or '\\' or '|' or '?' or '*' or ';'))
        {
            throw Validation($"{name} contains invalid characters.");
        }

        var baseName = Path.GetFileNameWithoutExtension(value);
        if (ReservedDeviceNames.Contains(baseName))
        {
            throw Validation($"{name} contains invalid characters.");
        }
    }

    private static DomainException Validation(string message) => new(ApiCodes.ValidationError, message);
}
