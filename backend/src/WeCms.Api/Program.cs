using WeCms.Api.Extensions;
using WeCms.Api.Json;
using WeCms.Api.Middleware;
using WeCms.Modules.System.Auth;
using WeCms.Modules.System.Permissions;
using WeCms.Modules.System.System;
using WeCms.Persistence.Data;

if (await OpenApiExtensions.ExportOpenApiAsync(args))
{
    return;
}

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.AddWeCmsPersistence(builder.Configuration);
builder.Services.AddWeCmsSystemAuth(builder.Configuration);
builder.Services.AddWeCmsSystemPermissions();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, WeCmsJsonSerializerContext.Default);
});

var app = builder.Build();

app.UseMiddleware<RequestIdMiddleware>();
app.UseMiddleware<ExceptionMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapSystemEndpoints();
app.MapAuthEndpoints();
app.MapSystemPermissionEndpoints();

app.Run();
