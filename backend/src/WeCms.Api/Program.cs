using System.Diagnostics;
using System.Net.Sockets;
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
using WeCms.Infrastructure.Id;
using WeCms.Shared;
using WeCms.Shared.Id;
using WeCms.Shared.Security;

if (await OpenApiExtensions.ExportOpenApiAsync(args))
{
    return;
}

var builder = WebApplication.CreateSlimBuilder(args);
builder.WebHost.UseKestrelHttpsConfiguration();
var isMigrationCommand = DatabaseMigrationCommand.IsMigrationCommand(args);

ProductionConfigurationValidator.Validate(builder.Configuration, builder.Environment);

builder.Services.AddWeCmsPersistence(builder.Configuration, useMigrationConnectionString: isMigrationCommand);
builder.Services.AddWeCmsFileStorage(builder.Configuration, builder.Environment);
builder.Services.AddSingleton<IIdGenerator, SystemIdGenerator>();
builder.Services.AddSingleton<IIpRuleMatcher, IpRuleMatcher>();
builder.Services.AddSingleton<ISecurityEventClassifier, SecurityEventClassifier>();
builder.Services.AddWeCmsSystemAuth(builder.Configuration);
builder.Services.AddWeCmsSystemDepartments();
builder.Services.AddWeCmsSystemDicts();
builder.Services.AddWeCmsSystemFiles(_ => CreateFileScanService(builder.Configuration));
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

StartFrontendDevServer(app, isMigrationCommand);

app.UseWeCmsForwardedHeaders(builder.Configuration);
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}
app.UseMiddleware<RequestIdMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
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

static void StartFrontendDevServer(WebApplication app, bool isMigrationCommand)
{
    if (isMigrationCommand || !app.Environment.IsDevelopment() || IsTcpPortOpen("127.0.0.1", 5173))
    {
        return;
    }

    var frontendRoot = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "..", "..", "..", "frontend", "soybean-admin"));
    if (!File.Exists(Path.Combine(frontendRoot, "package.json")))
    {
        return;
    }

    var isWindows = OperatingSystem.IsWindows();
    var process = new Process
    {
        StartInfo = new ProcessStartInfo
        {
            FileName = isWindows ? "cmd.exe" : "corepack",
            WorkingDirectory = frontendRoot,
            UseShellExecute = false,
            CreateNoWindow = true
        },
        EnableRaisingEvents = true
    };
    if (isWindows)
    {
        process.StartInfo.ArgumentList.Add("/c");
        process.StartInfo.ArgumentList.Add("corepack");
    }

    process.StartInfo.ArgumentList.Add("pnpm@10.5.0");
    process.StartInfo.ArgumentList.Add("exec");
    process.StartInfo.ArgumentList.Add("vite");
    process.StartInfo.ArgumentList.Add("--host");
    process.StartInfo.ArgumentList.Add("127.0.0.1");
    process.StartInfo.ArgumentList.Add("--port");
    process.StartInfo.ArgumentList.Add("5173");
    process.StartInfo.ArgumentList.Add("--strictPort");

    var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("FrontendDevServer");
    if (!process.Start())
    {
        return;
    }

    app.Lifetime.ApplicationStopping.Register(() =>
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
        }
    });
}

static bool IsTcpPortOpen(string host, int port)
{
    try
    {
        using var client = new TcpClient();
        return client.ConnectAsync(host, port).Wait(TimeSpan.FromMilliseconds(500));
    }
    catch (SocketException)
    {
        return false;
    }
}
static IFileScanService CreateFileScanService(IConfiguration configuration)
{
    if (!configuration.GetValue("FileStorage:VirusScanEnabled", false))
    {
        return new WeCms.Shared.NoopFileScanService();
    }

    var provider = configuration["FileStorage:VirusScan:Provider"];
    if (!string.Equals(provider, "clamav-tcp", StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException("FileStorage:VirusScan:Provider must be clamav-tcp when virus scanning is enabled.");
    }

    var host = configuration["FileStorage:VirusScan:Host"];
    var port = configuration.GetValue("FileStorage:VirusScan:Port", ClamAvFileScanOptions.DefaultPort);
    var timeoutSeconds = configuration.GetValue("FileStorage:VirusScan:TimeoutSeconds", ClamAvFileScanOptions.DefaultTimeoutSeconds);
    var chunkSizeBytes = configuration.GetValue("FileStorage:VirusScan:ChunkSizeBytes", ClamAvFileScanOptions.DefaultChunkSizeBytes);
    return new ClamAvFileScanService(new ClamAvFileScanOptions(host ?? string.Empty, port, timeoutSeconds, chunkSizeBytes));
}
