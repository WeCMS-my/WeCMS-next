using Microsoft.Extensions.DependencyInjection;
using WeCms.Api.Middleware;
using WeCms.Infrastructure.Migration;

var builder = WebApplication.CreateSlimBuilder(args);

// Register infrastructure services (DB, password hasher, clock, migration runner)
builder.Services.AddWeCmsInfrastructure();

var app = builder.Build();

// M0-BE-004: Middleware pipeline — RequestId first (trace propagation), then Exception (error handling)
app.UseMiddleware<RequestIdMiddleware>();
app.UseMiddleware<ExceptionMiddleware>();

// M0-BE-006: Run database migrations and seed on startup
using (var scope = app.Services.CreateScope())
{
    var migrator = scope.ServiceProvider.GetRequiredService<DbMigrationRunner>();
    await migrator.RunAsync();
}

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
