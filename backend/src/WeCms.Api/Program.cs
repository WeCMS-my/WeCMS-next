using WeCms.Api.Extensions;
using WeCms.Api.Json;
using WeCms.Api.Middleware;
using WeCms.Modules.System.Auth;
using WeCms.Modules.System.Departments;
using WeCms.Modules.System.Dicts;
using WeCms.Modules.System.Files;
using WeCms.Modules.System.Logs;
using WeCms.Modules.System.Menus;
using WeCms.Modules.System.Permissions;
using WeCms.Modules.System.Posts;
using WeCms.Modules.System.Roles;
using WeCms.Modules.System.Settings;
using WeCms.Modules.System.System;
using WeCms.Modules.System.Users;
using WeCms.Persistence.Data;
using WeCms.Infrastructure.Files;
using WeCms.Shared;

if (await OpenApiExtensions.ExportOpenApiAsync(args))
{
    return;
}

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.AddWeCmsPersistence(builder.Configuration);
builder.Services.AddScoped<IFileStorage, LocalFileStorage>();
builder.Services.AddWeCmsSystemAuth(builder.Configuration);
builder.Services.AddWeCmsSystemDepartments();
builder.Services.AddWeCmsSystemDicts();
builder.Services.AddWeCmsSystemFiles();
builder.Services.AddWeCmsSystemLogs();
builder.Services.AddWeCmsSystemMenus();
builder.Services.AddWeCmsSystemPermissions();
builder.Services.AddWeCmsSystemPosts();
builder.Services.AddWeCmsSystemRoles();
builder.Services.AddWeCmsSystemSettings();
builder.Services.AddWeCmsSystemUsers();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, WeCmsJsonSerializerContext.Default);
});

var app = builder.Build();

app.UseMiddleware<RequestIdMiddleware>();
app.UseMiddleware<ExceptionMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapAuditLogEndpoints();
app.MapDepartmentEndpoints();
app.MapDictEndpoints();
app.MapFileEndpoints();
app.MapLoginLogEndpoints();
app.MapSystemEndpoints();
app.MapAuthEndpoints();
app.MapMenuEndpoints();
app.MapPermissionManagementEndpoints();
app.MapPostEndpoints();
app.MapSystemPermissionEndpoints();
app.MapRoleEndpoints();
app.MapSettingEndpoints();
app.MapSecurityEventEndpoints();
app.MapUserEndpoints();

app.Run();
