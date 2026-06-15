using WeCms.Shared;

namespace WeCms.Tests.Unit.Shared;

public sealed class ApiResultTests
{
    [Fact]
    public void Ok_ShouldReturnSuccessContract_WhenDataIsProvided()
    {
        var result = ApiResult<string>.Ok("pong", "trace-001");

        Assert.Equal(ApiCodes.Success, result.Code);
        Assert.Equal("success", result.Msg);
        Assert.Equal("pong", result.Data);
        Assert.Equal("trace-001", result.TraceId);
        Assert.Null(result.FieldErrors);
    }

    [Fact]
    public void Fail_ShouldReturnErrorContract_WhenValidationErrorsAreProvided()
    {
        var fieldErrors = new Dictionary<string, IReadOnlyList<string>>
        {
            ["username"] = ["用户名不能为空"]
        };

        var result = ApiResult<string>.Fail(
            ApiCodes.ValidationError,
            "参数验证失败",
            "trace-002",
            fieldErrors);

        Assert.Equal(ApiCodes.ValidationError, result.Code);
        Assert.Equal("参数验证失败", result.Msg);
        Assert.Null(result.Data);
        Assert.Equal("trace-002", result.TraceId);
        Assert.Same(fieldErrors, result.FieldErrors);
    }
}
