namespace WeCms.Data.SqlSugar;

internal static class SqlIdentifier
{
    public static string Require(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        var trimmed = value.Trim();
        if (!IsIdentifier(trimmed))
        {
            throw new ArgumentException("SQL identifier must contain only letters, digits, or underscores and must not start with a digit.", parameterName);
        }

        return trimmed;
    }

    private static bool IsIdentifier(string value)
    {
        if (value.Length == 0 || char.IsDigit(value[0]))
        {
            return false;
        }

        foreach (var character in value)
        {
            if (character != '_' && !char.IsAsciiLetterOrDigit(character))
            {
                return false;
            }
        }

        return true;
    }
}
