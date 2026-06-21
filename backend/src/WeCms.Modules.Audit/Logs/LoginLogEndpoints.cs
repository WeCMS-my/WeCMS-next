using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using WeCms.Shared;
using WeCms.Shared.Endpoints;

namespace WeCms.Modules.Audit.Logs;

public static class LoginLogEndpoints
{
    public static IEndpointRouteBuilder MapLoginLogEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/system")
            .WithEndpointModule("audit")
            .RequireAuthorization();

        group.MapGet("/login-logs", ListAsync).RequireEndpointPermission(LogPermissions.LoginLogList);
        group.MapGet("/login-logs/{id:long}", DetailAsync).RequireEndpointPermission(LogPermissions.LoginLogDetail);

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
