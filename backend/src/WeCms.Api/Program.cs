using System.Diagnostics;
using System.Net.Sockets;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using WeCms.Aop;
using WeCms.Api.Endpoints;
using WeCms.Api.Extensions;
using WeCms.Api.AccessProfiles;
using WeCms.Api.Files;
using WeCms.Api.Json;
using WeCms.Api.Middleware;
using WeCms.Api.RateLimiting;
using WeCms.Api.Configuration;
using WeCms.Api.Security;
using WeCms.Caching;
using WeCms.Data.SqlSugar;
using WeCms.EventBus;
using WeCms.EventBus.SqlSugar;
using WeCms.Modules.AccessControl;
using WeCms.Modules.AccessControl.AccessProfiles;
using WeCms.Modules.AccessControl.Permissions;
using WeCms.Modules.AccessControl.Repositories;
using WeCms.Modules.AccessControl.SqlSugar;
using WeCms.Modules.Audit;
using WeCms.Modules.Audit.SqlSugar;
using WeCms.Modules.Configuration;
using WeCms.Modules.Configuration.Settings;
using WeCms.Modules.Configuration.SqlSugar;
using WeCms.Modules.FileCenter;
using WeCms.Modules.FileCenter.Files;
using WeCms.Modules.FileCenter.SqlSugar;
using WeCms.Modules.Identity;
using WeCms.Modules.Identity.Endpoints;
using WeCms.Modules.Identity.Services;
using WeCms.Modules.Identity.SqlSugar;
using WeCms.Modules.Organization;
using WeCms.Modules.Organization.SqlSugar;
using WeCms.Modules.Platform;
using WeCms.Modules.Platform.SqlSugar;
using WeCms.Modules.Security;
using WeCms.Modules.Security.SqlSugar;
using WeCms.Modules.Organization.Departments;
using WeCms.Modules.Configuration.Dicts;
using WeCms.Modules.Configuration.I18n;
using WeCms.Modules.Platform.Permissions;
using WeCms.Modules.Organization.Positions;
using WeCms.Modules.Platform.System;
using WeCms.Infrastructure.Files;
using WeCms.Infrastructure.Id;
using WeCms.Shared;
using WeCms.Shared.Endpoints;
using WeCms.Shared.Id;
using WeCms.Shared.Security;

if (await OpenApiExtensions.ExportOpenApiAsync(args))
{
    return;
}

var builder = WebApplication.CreateSlimBuilder(args);
builder.WebHost.UseKestrel();
builder.WebHost.UseKestrelHttpsConfiguration();
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
builder.Host.ConfigureContainer<ContainerBuilder>(container => container.RegisterModule(new WeCmsAopModule()));
var isMigrationCommand = DatabaseMigrationCommand.IsMigrationCommand(args);

ProductionConfigurationValidator.Validate(builder.Configuration, builder.Environment);

builder.Services.AddWeCmsSqlSugarData(
    builder.Configuration,
    useMigrationConnectionString: isMigrationCommand,
    codeFirstEnvironmentName: builder.Environment.EnvironmentName);
builder.Services.AddWeCmsCaching();
builder.Services.AddWeCmsEventBus();
builder.Services.AddWeCmsEventBusSqlSugar();
builder.Services.AddWeCmsAccessControlSqlSugar();
builder.Services.AddWeCmsAuditSqlSugar();
builder.Services.AddWeCmsConfigurationSqlSugar();
builder.Services.AddWeCmsFileCenterSqlSugar();
builder.Services.AddWeCmsIdentitySqlSugar();
builder.Services.AddWeCmsOrganizationSqlSugar();
builder.Services.AddWeCmsPlatformSqlSugar();
builder.Services.AddWeCmsSecuritySqlSugar();
builder.Services.AddWeCmsFileStorage(builder.Configuration, builder.Environment);
builder.Services.AddSingleton<IIdGenerator, SystemIdGenerator>();
builder.Services.AddSingleton<IIpRuleMatcher, IpRuleMatcher>();
builder.Services.AddSingleton<ISecurityEventClassifier, SecurityEventClassifier>();
builder.Services.AddSingleton<SecurityRejectionEventBuffer>();
builder.Services.AddSingleton<ISecurityRejectionEventBuffer>(provider => provider.GetRequiredService<SecurityRejectionEventBuffer>());
builder.Services.AddSingleton<ISecurityRejectionEventReader>(provider => provider.GetRequiredService<SecurityRejectionEventBuffer>());
builder.Services.AddHostedService<SecurityRejectionEventFlushHostedService>();
builder.Services.AddWeCmsAccessControl();
builder.Services.AddScoped<IAccessProfileService>(provider => new CachedAccessProfileService(
    provider.GetRequiredService<AccessProfileService>(),
    provider.GetRequiredService<IAccessProfileRepository>(),
    provider.GetRequiredService<ICache>(),
    provider.GetRequiredService<ICacheKeyBuilder>()));
builder.Services.AddWeCmsAudit();
builder.Services.AddWeCmsConfiguration();
builder.Services.AddWeCmsIdentity(builder.Configuration);
builder.Services.AddWeCmsOrganization();
builder.Services.AddWeCmsPlatform();
builder.Services.AddWeCmsSecurity();
builder.Services.AddWeCmsFileCenter(_ => CreateFileScanService(builder.Configuration));
builder.Services.AddScoped<IAccountAvatarFileService, AccountAvatarFileService>();
builder.Services.AddScoped<PermissionVersionService>();
builder.Services.AddScoped<IAccessProfileCache, AccessProfileCache>();
builder.Services.AddScoped<ISecurityBanLookupCache, SecurityBanLookupCache>();
builder.Services.AddScoped<IAccessControlPermissionVersionService>(provider => provider.GetRequiredService<PermissionVersionService>());
builder.Services.AddScoped<IIdentityPermissionVersionService>(provider => provider.GetRequiredService<PermissionVersionService>());
builder.Services.AddScoped<IIdentitySecurityAlertService, IdentitySecurityAlertServiceAdapter>();
builder.Services.AddScoped<IIdentitySecurityBanService, IdentitySecurityBanServiceAdapter>();
builder.Services.AddWeCmsForwardedHeaders(builder.Configuration);
builder.Services.AddWeCmsCors(builder.Configuration);
builder.Services.AddWeCmsRateLimiting(builder.Configuration);
builder.Services.AddWeCmsOpenApiDocumentation(builder.Configuration, builder.Environment);
builder.Services.AddWeCmsDiagnostics(builder.Configuration, builder.Environment);
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

await StartFrontendDevServerAsync(app, isMigrationCommand);

app.UseWeCmsForwardedHeaders(builder.Configuration);
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}
app.UseMiddleware<RequestIdMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<SecureHeadersMiddleware>();
app.UseMiddleware<ExceptionMiddleware>();
app.UseWeCmsDiagnostics();
app.UseMiddleware<IpAccessControlMiddleware>();
app.UseCors(WeCmsCorsPolicyNames.AdminApi);
app.UseAuthentication();
app.UseMiddleware<SecurityBanMiddleware>();
app.UseRateLimiter();
app.UseAuthorization();

app.MapWeCmsApiEndpoints();
app.MapWeCmsOpenApiDocumentation();

app.Run();

static async Task StartFrontendDevServerAsync(WebApplication app, bool isMigrationCommand)
{
    if (isMigrationCommand || !app.Environment.IsDevelopment() || await IsTcpPortOpenAsync("127.0.0.1", 5173))
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
            logger.LogDebug("Frontend dev server process already exited before shutdown cleanup.");
        }
    });
}

static async Task<bool> IsTcpPortOpenAsync(string host, int port)
{
    try
    {
        using var client = new TcpClient();
        await client.ConnectAsync(host, port).WaitAsync(TimeSpan.FromMilliseconds(500));
        return true;
    }
    catch (SocketException)
    {
        return false;
    }
    catch (TimeoutException)
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
