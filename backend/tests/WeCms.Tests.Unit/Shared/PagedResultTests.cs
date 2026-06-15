using WeCms.Shared;

namespace WeCms.Tests.Unit.Shared;

public sealed class PagedResultTests
{
    [Fact]
    public void Constructor_ShouldExposePagingContract_WhenValuesAreProvided()
    {
        var records = new[] { "one", "two" };

        var result = new PagedResult<string>(records, 2, 20, 42);

        Assert.Same(records, result.Records);
        Assert.Equal(2, result.Page);
        Assert.Equal(20, result.PageSize);
        Assert.Equal(42, result.Total);
    }
}
