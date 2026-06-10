using WeCms.Shared;

namespace WeCms.Tests.Unit.Shared;

public sealed class ApiCodesTests
{
    [Fact]
    public void Success_ShouldBeZero() => Assert.Equal(0, ApiCodes.Success);

    [Fact]
    public void Unauthorized_ShouldBe401() => Assert.Equal(401, ApiCodes.Unauthorized);

    [Fact]
    public void Forbidden_ShouldBe403() => Assert.Equal(403, ApiCodes.Forbidden);

    [Fact]
    public void NotFound_ShouldBe404() => Assert.Equal(404, ApiCodes.NotFound);

    [Fact]
    public void ValidationError_ShouldBe1001() => Assert.Equal(1001, ApiCodes.ValidationError);

    [Fact]
    public void BusinessError_ShouldBe2001() => Assert.Equal(2001, ApiCodes.BusinessError);

    [Fact]
    public void SystemError_ShouldBe5000() => Assert.Equal(5000, ApiCodes.SystemError);
}
