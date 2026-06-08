 using WeCms.Shared;
 using Xunit;
 
 namespace WeCms.Tests.Unit;
 
 public class PagedResultTests
 {
     [Fact]
     public void Constructor_ShouldSetAllProperties()
     {
         var records = new[] { "a", "b", "c" };
         
         var result = new PagedResult<string>(records, 1, 10, 30);
         
         Assert.Equal(3, result.Records.Count);
         Assert.Equal(1, result.Page);
         Assert.Equal(10, result.PageSize);
         Assert.Equal(30, result.Total);
     }
 }
