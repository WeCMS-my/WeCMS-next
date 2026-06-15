using Microsoft.AspNetCore.Http;
using WeCms.Api.Middleware;

namespace WeCms.Tests.Integration.Middleware;

public sealed class RequestIdMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ShouldReuseIncomingRequestId_WhenHeaderIsProvided()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[RequestIdMiddleware.HeaderName] = "trace-test";
        var nextCalled = false;
        var middleware = new RequestIdMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
        Assert.Equal("trace-test", context.TraceIdentifier);
        Assert.Equal("trace-test", context.Response.Headers[RequestIdMiddleware.HeaderName]);
    }
}
