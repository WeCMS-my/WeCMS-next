using Microsoft.Extensions.Configuration;
using WeCms.Persistence.Data;

namespace WeCms.Tests.Unit.Persistence;

public sealed class DatabaseOptionsTests
{
    [Fact]
    public void FromConfiguration_UsesDefaultCommandTimeoutWhenMissing()
    {
        var options = DatabaseOptions.FromConfiguration(Configuration(new Dictionary<string, string?>()));

        Assert.Equal(DatabaseOptions.DefaultCommandTimeoutSeconds, options.CommandTimeoutSeconds);
    }

    [Fact]
    public void FromConfiguration_AcceptsConfiguredCommandTimeout()
    {
        var options = DatabaseOptions.FromConfiguration(Configuration(new Dictionary<string, string?>
        {
            ["Database:CommandTimeoutSeconds"] = "45"
        }));

        Assert.Equal(45, options.CommandTimeoutSeconds);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("301")]
    [InlineData("abc")]
    public void FromConfiguration_RejectsInvalidCommandTimeout(string value)
    {
        var exception = Assert.Throws<PersistenceConfigurationException>(
            () => DatabaseOptions.FromConfiguration(Configuration(new Dictionary<string, string?>
            {
                ["Database:CommandTimeoutSeconds"] = value
            })));

        Assert.Equal("Database:CommandTimeoutSeconds must be an integer between 1 and 300.", exception.Message);
    }

    private static IConfiguration Configuration(IReadOnlyDictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
