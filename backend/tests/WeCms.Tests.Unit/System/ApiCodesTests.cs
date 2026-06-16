using WeCms.Shared;

namespace WeCms.Tests.Unit.SystemApi;

public sealed class ApiCodesTests
{
    [Fact]
    public void ServiceUnavailable_MapsToHttp503()
    {
        Assert.Equal(503, ApiCodes.ToHttpStatus(ApiCodes.ServiceUnavailable));
    }
}
