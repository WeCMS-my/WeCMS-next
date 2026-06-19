using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace WeCms.Persistence.Data;

public sealed record DatabaseOptions(int CommandTimeoutSeconds)
{
    public const int DefaultCommandTimeoutSeconds = 30;
    public const int MinimumCommandTimeoutSeconds = 1;
    public const int MaximumCommandTimeoutSeconds = 300;

    public static DatabaseOptions FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var commandTimeout = ReadInt(
            configuration,
            "Database:CommandTimeoutSeconds",
            DefaultCommandTimeoutSeconds,
            MinimumCommandTimeoutSeconds,
            MaximumCommandTimeoutSeconds);

        return new DatabaseOptions(commandTimeout);
    }

    private static int ReadInt(
        IConfiguration configuration,
        string key,
        int defaultValue,
        int minimum,
        int maximum)
    {
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed)
            || parsed < minimum
            || parsed > maximum)
        {
            throw new PersistenceConfigurationException($"{key} must be an integer between {minimum} and {maximum}.");
        }

        return parsed;
    }
}
