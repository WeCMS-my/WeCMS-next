using WeCms.Shared;

namespace WeCms.Tests.Unit.Shared;

public sealed class ApiResultTests
{
    [Fact]
    public void Ok_ShouldReturnSuccess_WhenGivenData()
    {
        var result = ApiResult<string>.Ok("hello");
        Assert.Equal(0, result.Code);
        Assert.Equal("success", result.Msg);
        Assert.Equal("hello", result.Data);
        Assert.Null(result.TraceId);
        Assert.Null(result.FieldErrors);
    }

    [Fact]
    public void Ok_ShouldReturnSuccess_WithTraceId()
    {
        var result = ApiResult<int>.Ok(42, "trace-123");
        Assert.Equal(0, result.Code);
        Assert.Equal(42, result.Data);
        Assert.Equal("trace-123", result.TraceId);
    }

    [Fact]
    public void Fail_ShouldReturnError_WithCodeAndMessage()
    {
        var result = ApiResult<string>.Fail(1001, "参数验证失败");
        Assert.Equal(1001, result.Code);
        Assert.Equal("参数验证失败", result.Msg);
        Assert.Null(result.Data);
        Assert.Null(result.TraceId);
        Assert.Null(result.FieldErrors);
    }

    [Fact]
    public void Fail_ShouldReturnError_WithFieldErrors()
    {
        var fieldErrors = new Dictionary<string, string[]>
        {
            ["username"] = new[] { "用户名不能为空" }
        };
        var result = ApiResult<string>.Fail(1001, "参数验证失败", "trace-456", fieldErrors);
        Assert.Equal(1001, result.Code);
        Assert.Equal("trace-456", result.TraceId);
        Assert.NotNull(result.FieldErrors);
        Assert.Single(result.FieldErrors!["username"]);
    }
}
