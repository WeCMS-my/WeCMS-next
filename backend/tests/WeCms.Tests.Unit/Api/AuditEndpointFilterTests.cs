using Microsoft.AspNetCore.Http;
using System.Net;
using System.Security.Claims;
using WeCms.Api.Endpoints;
using WeCms.Shared.Endpoints;

namespace WeCms.Tests.Unit.Api;

public sealed class AuditEndpointFilterTests
{
    [Fact]
    public async Task AuditEndpointFilter_WritesSuccessAudit()
    {
        var writer = new RecordingAuditWriter();
        var httpContext = CreateHttpContext(new EndpointAuditMetadata("identity", "users", "create"));
        var context = new TestEndpointFilterInvocationContext(httpContext);
        var filter = new AuditEndpointFilter(writer);
        var called = false;

        var result = await filter.InvokeAsync(context, _ =>
        {
            called = true;
            return ValueTask.FromResult<object?>("ok");
        });

        Assert.True(called);
        Assert.Equal("ok", result);
        Assert.Collection(
            writer.Records,
            started => AssertAudit(started, AuditWriteStatus.Started),
            completed => AssertAudit(completed, AuditWriteStatus.Completed));
    }

    [Fact]
    public async Task AuditEndpointFilter_WritesFailureAudit()
    {
        var writer = new RecordingAuditWriter();
        var httpContext = CreateHttpContext(new EndpointAuditMetadata("identity", "users", "create"));
        var context = new TestEndpointFilterInvocationContext(httpContext);
        var filter = new AuditEndpointFilter(writer);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await filter.InvokeAsync(context, _ => throw new InvalidOperationException("boom")));

        Assert.Equal("boom", exception.Message);
        Assert.Collection(
            writer.Records,
            started => AssertAudit(started, AuditWriteStatus.Started),
            failed =>
            {
                AssertAudit(failed, AuditWriteStatus.Failed);
                Assert.Equal("boom", failed.Detail);
            });
    }

    [Fact]
    public async Task AuditEndpointFilter_WritesRequestContext()
    {
        var writer = new RecordingAuditWriter();
        var httpContext = CreateHttpContext(new EndpointAuditMetadata("identity", "users", "update"));
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "42"),
                new Claim(ClaimTypes.Name, "admin")
            ],
            "unit"));
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("192.0.2.10");
        httpContext.Request.Headers.UserAgent = "wecms-unit";
        httpContext.Request.RouteValues["id"] = 1001L;
        var context = new TestEndpointFilterInvocationContext(httpContext);
        var filter = new AuditEndpointFilter(writer);

        await filter.InvokeAsync(context, _ => ValueTask.FromResult<object?>("ok"));

        var completed = Assert.Single(writer.Records, static record => record.Status == AuditWriteStatus.Completed);
        Assert.Equal(42, completed.UserId);
        Assert.Equal("admin", completed.Username);
        Assert.Equal("192.0.2.10", completed.IpAddress);
        Assert.Equal("wecms-unit", completed.UserAgent);
        Assert.Equal("1001", completed.TargetId);
    }

    [Fact]
    public async Task AuditEndpointFilter_Skips_WhenNoAuditMetadata()
    {
        var writer = new RecordingAuditWriter();
        var httpContext = CreateHttpContext(metadata: null);
        var context = new TestEndpointFilterInvocationContext(httpContext);
        var filter = new AuditEndpointFilter(writer);
        var called = false;

        var result = await filter.InvokeAsync(context, _ =>
        {
            called = true;
            return ValueTask.FromResult<object?>("ok");
        });

        Assert.True(called);
        Assert.Equal("ok", result);
        Assert.Empty(writer.Records);
    }

    private static DefaultHttpContext CreateHttpContext(EndpointAuditMetadata? metadata)
    {
        var httpContext = new DefaultHttpContext
        {
            TraceIdentifier = "trace-audit"
        };
        httpContext.Request.Method = HttpMethods.Post;
        httpContext.Request.Path = "/api/v1/system/users";

        if (metadata is not null)
        {
            httpContext.SetEndpoint(new Endpoint(
                _ => Task.CompletedTask,
                new EndpointMetadataCollection(metadata),
                "audit-test"));
        }

        return httpContext;
    }

    private static void AssertAudit(AuditWriteRecord record, AuditWriteStatus status)
    {
        Assert.Equal("identity", record.Module);
        Assert.Equal("users", record.Resource);
        Assert.Equal("create", record.Action);
        Assert.Equal(status, record.Status);
        Assert.Equal("POST", record.RequestMethod);
        Assert.Equal("/api/v1/system/users", record.RequestPath);
        Assert.Equal("trace-audit", record.TraceId);
    }

    private sealed class RecordingAuditWriter : IAuditWriter
    {
        public List<AuditWriteRecord> Records { get; } = [];

        public ValueTask WriteAsync(AuditWriteRecord record, CancellationToken cancellationToken)
        {
            Records.Add(record);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class TestEndpointFilterInvocationContext : EndpointFilterInvocationContext
    {
        public TestEndpointFilterInvocationContext(HttpContext httpContext)
        {
            HttpContext = httpContext;
        }

        public override HttpContext HttpContext { get; }

        public override IList<object?> Arguments { get; } = [];

        public override T GetArgument<T>(int index)
        {
            return (T)Arguments[index]!;
        }
    }
}
