using WeCms.Shared;

namespace WeCms.Tests.Unit.Shared;

public sealed class PagedResultTests
{
    [Fact]
    public void Constructor_ShouldSetAllProperties()
    {
        var records = new List<string> { "a", "b", "c" }.AsReadOnly();
        var result = new PagedResult<string>(records, 1, 10, 100L);

        Assert.Equal(3, result.Records.Count);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(100L, result.Total);
    }

    [Fact]
    public void Constructor_ShouldHandleEmptyRecords()
    {
        var records = Array.Empty<string>().AsReadOnly();
        var result = new PagedResult<string>(records, 1, 20, 0L);

        Assert.Empty(result.Records);
        Assert.Equal(0L, result.Total);
    }
}
