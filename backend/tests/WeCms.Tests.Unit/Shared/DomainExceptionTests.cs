using WeCms.Shared;

namespace WeCms.Tests.Unit.Shared;

public sealed class DomainExceptionTests
{
    [Fact]
    public void Constructor_ShouldSetProperties()
    {
        var ex = new DomainException(2001, "业务错误");

        Assert.Equal(2001, ex.Code);
        Assert.Equal("业务错误", ex.Message);
    }

    [Fact]
    public void ShouldBeThrowable()
    {
        static void ThrowAction() => throw new DomainException(5000, "系统错误");
        var ex = Assert.Throws<DomainException>(ThrowAction);

        Assert.Equal(5000, ex.Code);
    }
}
