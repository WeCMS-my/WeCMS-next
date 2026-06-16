using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using WeCms.Modules.System.Permissions;
using WeCms.Shared;

namespace WeCms.Modules.System.Logs;

public static class LoginLogEndpoints
{
    public static IEndpointRouteBuilder MapLoginLogEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/system")
            .RequireAuthorization();

        group.MapGet("/login-logs", ListAsync).RequirePermission(LogPermissions.LoginLogList);
        group.MapGet("/login-logs/{id:long}", DetailAsync).RequirePermission(LogPermissions.LoginLogDetail);

        return endpoints;
    }

    private static async Task<ApiResult<PagedResult<LoginLogSummaryDto>>> ListAsync(int page, int pageSize, string? username, string? ip, string? result, DateTimeOffset? from, DateTimeOffset? to, ILogService service, CancellationToken cancellationToken)
    {
        return ApiResult<PagedResult<LoginLogSummaryDto>>.Ok(await service.ListLoginLogsAsync(new LoginLogListQuery(page, pageSize, username, ip, result, from, to), cancellationToken));
    }

    private static async Task<ApiResult<LoginLogDetailDto>> DetailAsync(long id, ILogService service, CancellationToken cancellationToken)
    {
        return ApiResult<LoginLogDetailDto>.Ok(await service.GetLoginLogAsync(id, cancellationToken));
    }
}
