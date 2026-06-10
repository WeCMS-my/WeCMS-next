using WeCms.Api.Middleware;

var builder = WebApplication.CreateSlimBuilder(args);
var app = builder.Build();

// M0-BE-004: Middleware pipeline — RequestId first (trace propagation), then Exception (error handling)
app.UseMiddleware<RequestIdMiddleware>();
app.UseMiddleware<ExceptionMiddleware>();

app.MapGet("/", () => "Hello World!");

// Test endpoints for middleware verification
app.MapGet("/api/v1/system/ping", () => Results.Ok(new { status = "pong" }));

app.MapGet("/test/throw-domain-exception", () =>
{
    throw new WeCms.Shared.DomainException(2001, "测试业务异常");
});

app.MapGet("/test/throw-exception", () =>
{
    throw new InvalidOperationException("测试未处理异常");
});

app.Run();
