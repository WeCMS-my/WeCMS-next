using WeCms.Modules.System.Permissions;
using WeCms.Modules.System.I18n;
using WeCms.Modules.System.Security;
 using WeCms.Shared.Contracts;
 using WeCms.Infrastructure.Data;
 using WeCms.Infrastructure.Security;
 using WeCms.Modules.System.Auth;
 using WeCms.Modules.System.Users;
 using WeCms.Modules.System.Roles;
 using WeCms.Modules.System.Menus;
 using WeCms.Modules.System.Settings;
 using WeCms.Modules.System.Dicts;
 using WeCms.Modules.System.Files;
 using WeCms.Modules.System.Logs;
 
 namespace WeCms.Api.Extensions;
 
 public static class ServiceCollectionExtensions
 {
     public static IServiceCollection AddWeCmsInfrastructure(this IServiceCollection services, IConfiguration configuration)
     {
         var cs = configuration.GetConnectionString("Default") ?? throw new InvalidOperationException("DB connection string required");
         services.AddSingleton<IDbConnectionFactory>(new DbConnectionFactory(cs));
         var secret = configuration["Auth:JwtSecret"] ?? throw new InvalidOperationException("Auth:JwtSecret required");
         services.AddSingleton<ITokenService>(new TokenService(secret, 900));
         services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
         services.AddSingleton<ITwoFactorService, TwoFactorService>();
         services.AddScoped<IAuthService, AuthService>();
         services.AddScoped<ISecurityEventLogger, SecurityEventLogger>();
         services.AddScoped<UserService>();
         services.AddScoped<RoleService>();
         services.AddScoped<MenuService>();
         services.AddScoped<SettingService>();
         services.AddScoped<DictService>();
         services.AddScoped<FileService>();
         services.AddScoped<LogService>();
        services.AddScoped<PermissionSyncService>();
        services.AddScoped<I18nService>();
        services.AddScoped<SecurityService>();
         return services;
     }
 }
