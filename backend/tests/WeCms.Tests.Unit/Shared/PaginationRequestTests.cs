using WeCms.Shared.Pagination;

namespace WeCms.Tests.Unit.Shared;

public sealed class PaginationRequestTests
{
    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        var req = new PaginationRequest();
        Assert.Equal(1, req.Page);
        Assert.Equal(20, req.PageSize);
    }

    [Fact]
    public void Constructor_ShouldSetCustomValues()
    {
        var req = new PaginationRequest(2, 50);
        Assert.Equal(2, req.Page);
        Assert.Equal(50, req.PageSize);
    }

    [Fact]
    public void MetadataOnly_ShouldBeOneBased()
    {
        var req = PaginationRequest.FirstPage;
        Assert.Equal(1, req.Page);
        Assert.Equal(20, req.PageSize);
    }
}
