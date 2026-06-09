using Microsoft.Extensions.Caching.Memory;
using WeCms.Shared;
using WeCms.Shared.Contracts;

namespace WeCms.Modules.System;

public sealed class PermissionEndpointFilter(IDbConnectionFactory db, IMemoryCache cache) : IEndpointFilter
{
    private static readonly MemoryCacheEntryOptions CacheEntryOptions = new()
    {
        SlidingExpiration = TimeSpan.FromMinutes(5)
    };

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var ep = context.HttpContext.GetEndpoint();
        var meta = ep?.Metadata.GetMetadata<PermissionMetadata>();
        if (meta is null) return await next(context);

        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated != true)
            return Results.Ok(ApiResult<string>.Fail(ApiCodes.Unauthorized, "Authentication required"));

        var uidClaim = user.FindFirst("sub")?.Value;
        if (uidClaim is null || !long.TryParse(uidClaim, out var uid))
            return Results.Ok(ApiResult<string>.Fail(ApiCodes.Unauthorized, "Invalid token"));

        // In-memory permission cache key: perm:{uid}:{code}:{permissionVersion}
        var permissionVersion = user.FindFirst("permission_version")?.Value ?? "0";
        var cacheKey = $"perm:{uid}:{meta.Code}:{permissionVersion}";

        if (cache.TryGetValue<bool>(cacheKey, out var cached))
            return cached ? await next(context) : Results.Ok(ApiResult<string>.Fail(ApiCodes.Forbidden, "Insufficient permissions"));

        await using var conn = await db.OpenAsync(context.HttpContext.RequestAborted);

        var hasPermission = await conn.ExecuteScalarAsync<int>(new CommandDefinition("""
            SELECT COUNT(1) FROM sys_permission p
            JOIN sys_role_permission rp ON rp.permission_id=p.id
            JOIN sys_user_role ur ON ur.role_id=rp.role_id
            JOIN sys_role r ON r.id=ur.role_id AND r.status='active' AND r.deleted_at IS NULL
            WHERE ur.user_id=@Uid AND p.code=@Code AND p.status='active'
            """,
            new { Uid = uid, Code = meta.Code }, cancellationToken: context.HttpContext.RequestAborted));

        var result = hasPermission > 0;
        cache.Set(cacheKey, result, CacheEntryOptions);

        return result
            ? await next(context)
            : Results.Ok(ApiResult<string>.Fail(ApiCodes.Forbidden, "Insufficient permissions"));
    }
}
