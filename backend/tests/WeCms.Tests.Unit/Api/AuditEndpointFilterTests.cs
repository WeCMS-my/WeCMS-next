using Microsoft.AspNetCore.Http;
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
