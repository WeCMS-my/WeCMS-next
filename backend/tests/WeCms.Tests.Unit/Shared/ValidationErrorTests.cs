using WeCms.Shared;

namespace WeCms.Tests.Unit.Shared;

public sealed class ValidationErrorTests
{
    [Fact]
    public void Constructor_ShouldExposeFieldAndMessages_WhenValuesAreProvided()
    {
        var messages = new[] { "用户名不能为空" };

        var error = new ValidationError("username", messages);

        Assert.Equal("username", error.Field);
        Assert.Same(messages, error.Messages);
    }
}
