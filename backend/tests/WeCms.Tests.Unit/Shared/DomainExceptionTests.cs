using WeCms.Shared;

namespace WeCms.Tests.Unit.Shared;

public sealed class DomainExceptionTests
{
    [Fact]
    public void Constructor_ShouldExposeErrorCodeAndStatusCode_WhenValuesAreProvided()
    {
        var exception = new DomainException(
            ApiCodes.Conflict,
            "数据版本冲突",
            409);

        Assert.Equal(ApiCodes.Conflict, exception.Code);
        Assert.Equal("数据版本冲突", exception.Message);
        Assert.Equal(409, exception.StatusCode);
    }
}
