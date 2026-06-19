using WeCms.Api.Extensions;
using WeCms.Api.Json;
using WeCms.Api.Middleware;
using WeCms.Api.RateLimiting;
using WeCms.Api.Configuration;
using WeCms.Api.Security;
using WeCms.Modules.System.Auth;
using WeCms.Modules.System.Departments;
using WeCms.Modules.System.Dicts;
using WeCms.Modules.System.Files;
using WeCms.Modules.System.I18n;
using WeCms.Modules.System.Logs;
using WeCms.Modules.System.Menus;
using WeCms.Modules.System.Permissions;
using WeCms.Modules.System.Posts;
using WeCms.Modules.System.Roles;
using WeCms.Modules.System.Security;
using WeCms.Modules.System.Settings;
using WeCms.Modules.System.System;
using WeCms.Modules.System.TwoFactor;
using WeCms.Modules.System.Users;
using WeCms.Persistence.Data;
using WeCms.Infrastructure.Files;
using WeCms.Shared;
using WeCms.Shared.Security;

if (await OpenApiExtensions.ExportOpenApiAsync(args))
{
    return;
}

var builder = WebApplication.CreateSlimBuilder(args);
var isMigrationCommand = DatabaseMigrationCommand.IsMigrationCommand(args);

ProductionConfigurationValidator.Validate(builder.Configuration, builder.Environment);

builder.Services.AddWeCmsPersistence(builder.Configuration, useMigrationConnectionString: isMigrationCommand);
builder.Services.AddScoped<IFileStorage, LocalFileStorage>();
builder.Services.AddSingleton<IIpRuleMatcher, IpRuleMatcher>();
builder.Services.AddSingleton<ISecurityEventClassifier, SecurityEventClassifier>();
builder.Services.AddWeCmsSystemAuth(builder.Configuration);
builder.Services.AddWeCmsSystemDepartments();
builder.Services.AddWeCmsSystemDicts();
builder.Services.AddWeCmsSystemFiles();
builder.Services.AddWeCmsSystemI18n();
builder.Services.AddWeCmsSystemLogs();
builder.Services.AddWeCmsSystemMenus();
builder.Services.AddWeCmsSystemPermissions();
builder.Services.AddWeCmsSystemPosts();
builder.Services.AddWeCmsSystemRoles();
builder.Services.AddWeCmsSystemSecurity();
builder.Services.AddWeCmsSystemSettings();
builder.Services.AddWeCmsSystemTwoFactor(builder.Configuration);
builder.Services.AddWeCmsSystemUsers();
builder.Services.AddWeCmsForwardedHeaders(builder.Configuration);
builder.Services.AddWeCmsCors(builder.Configuration);
builder.Services.AddWeCmsRateLimiting(builder.Configuration);
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, WeCmsJsonSerializerContext.Default);
});

var app = builder.Build();

if (isMigrationCommand)
{
    await DatabaseMigrationCommand.RunAsync(app);
    return;
}

if (DatabaseStartupMigrationOptions.ShouldRunMigrationsOnStartup(builder.Configuration, app.Environment))
{
    await DatabaseMigrationCommand.RunAsync(app);
}

app.UseWeCmsForwardedHeaders(builder.Configuration);
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseMiddleware<RequestIdMiddleware>();
app.UseMiddleware<SecureHeadersMiddleware>();
app.UseMiddleware<ExceptionMiddleware>();
app.UseMiddleware<IpAccessControlMiddleware>();
app.UseCors(WeCmsCorsPolicyNames.AdminApi);
app.UseAuthentication();
app.UseMiddleware<SecurityBanMiddleware>();
app.UseRateLimiter();
app.UseAuthorization();

app.MapAuditLogEndpoints();
app.MapDepartmentEndpoints();
app.MapDictEndpoints();
app.MapFileEndpoints();
app.MapI18nEndpoints();
app.MapLoginLogEndpoints();
app.MapSystemEndpoints();
app.MapAuthEndpoints();
app.MapAccountProfileEndpoints();
app.MapAccountTwoFactorEndpoints();
app.MapMenuEndpoints();
app.MapPermissionManagementEndpoints();
app.MapPostEndpoints();
app.MapSystemPermissionEndpoints();
app.MapRoleEndpoints();
app.MapSettingEndpoints();
app.MapSecurityEndpoints();
app.MapSecurityEventEndpoints();
app.MapUserEndpoints();

app.Run();
