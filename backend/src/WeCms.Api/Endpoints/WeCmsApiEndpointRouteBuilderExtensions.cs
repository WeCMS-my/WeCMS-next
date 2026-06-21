using WeCms.Modules.AccessControl;
using WeCms.Modules.Audit;
using WeCms.Modules.Configuration.Dicts;
using WeCms.Modules.Configuration.I18n;
using WeCms.Modules.Configuration.Settings;
using WeCms.Modules.FileCenter;
using WeCms.Modules.Identity.Endpoints;
using WeCms.Modules.Organization.Departments;
using WeCms.Modules.Organization.Positions;
using WeCms.Modules.Platform;
using WeCms.Modules.Security;
using WeCms.Shared.Endpoints;

namespace WeCms.Api.Endpoints;

public static class WeCmsApiEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapWeCmsApiEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapAuditEndpoints();
        endpoints.MapEndpointDefinitions(registry =>
        {
            registry.Add(new AuthEndpointDefinition());
            registry.Add(new AccountProfileEndpointDefinition());
            registry.Add(new AccountTwoFactorEndpointDefinition());
            registry.Add(new UserEndpointDefinition());
        });
        endpoints.MapDepartmentEndpoints();
        endpoints.MapDictEndpoints();
        endpoints.MapFileCenterEndpoints();
        endpoints.MapI18nEndpoints();
        endpoints.MapPlatformEndpoints();
        endpoints.MapAccessControlEndpoints();
        endpoints.MapPositionEndpoints();
        endpoints.MapSettingEndpoints();
        endpoints.MapSecurityEndpoints();

        return endpoints;
    }
}
