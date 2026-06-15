using WeCms.Persistence.Data;
using WeCms.Shared;

namespace WeCms.Tests.Unit.Persistence;

public sealed class MySqlPersistenceOptionsTests
{
    [Fact]
    public void Validate_ShouldFailFast_WhenConnectionStringIsMissing()
    {
        var options = new MySqlPersistenceOptions("");

        var exception = Assert.Throws<DomainException>(options.Validate);

        Assert.Equal(ApiCodes.InvalidConfiguration, exception.Code);
        Assert.Equal(500, exception.StatusCode);
        Assert.Contains("ConnectionStrings:WeCms", exception.Message);
    }

    [Fact]
    public void Validate_ShouldReturnSameOptions_WhenConnectionStringIsProvided()
    {
        var options = new MySqlPersistenceOptions("Server=localhost;Database=wecms_next;");

        Assert.Same(options, options.Validate());
    }
}
