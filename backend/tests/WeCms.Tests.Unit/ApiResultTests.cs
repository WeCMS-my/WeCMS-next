 using WeCms.Shared;
 using Xunit;
 
 namespace WeCms.Tests.Unit;
 
 public class ApiResultTests
 {
     [Fact]
     public void Ok_ShouldReturnSuccessCodeAndData()
     {
         var data = new { name = "test" };
         
         var result = ApiResult<object>.Ok(data);
         
         Assert.Equal(0, result.Code);
         Assert.Equal("success", result.Msg);
         Assert.Same(data, result.Data);
     }
 
     [Fact]
     public void Fail_ShouldReturnSpecifiedCodeAndMessage()
     {
         var result = ApiResult<string>.Fail(1001, "validation error");
         
         Assert.Equal(1001, result.Code);
         Assert.Equal("validation error", result.Msg);
         Assert.Null(result.Data);
     }
 
     [Fact]
     public void Ok_WithValueType_ShouldReturnCorrectValue()
     {
         var result = ApiResult<int>.Ok(42);
         
         Assert.Equal(0, result.Code);
         Assert.Equal("success", result.Msg);
         Assert.Equal(42, result.Data);
     }
 }
