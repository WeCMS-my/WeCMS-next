using Microsoft.AspNetCore.Http.Features;

namespace WeCms.Tests.Unit.Api;

internal sealed class AbortedRequestLifetimeFeature : IHttpRequestLifetimeFeature
{
    public bool AbortCalled { get; private set; }

    public CancellationToken RequestAborted { get; set; }

    public void Abort()
    {
        AbortCalled = true;
    }
}
