 using WeCms.Shared;
 using WeCms.Shared.Contracts;
 
 namespace WeCms.Modules.System;
 
 public sealed class PermissionEndpointFilter(IDbConnectionFactory db) : IEndpointFilter
 {
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
 
         // Check is_super_admin from JWT claims first (cached, no DB query)
         var isSuperClaim = user.FindFirst("is_super_admin")?.Value;
         if (isSuperClaim == "true") return await next(context);
 
         await using var conn = await db.OpenAsync(context.HttpContext.RequestAborted);
 
         // Check permission
         var hasPermission = await conn.ExecuteScalarAsync<int>(new CommandDefinition("""
             SELECT COUNT(1) FROM sys_permission p
             JOIN sys_role_permission rp ON rp.permission_id=p.id
             JOIN sys_user_role ur ON ur.role_id=rp.role_id
             WHERE ur.user_id=@Uid AND p.code=@Code AND p.status='active'
             """,
             new { Uid = uid, Code = meta.Code }, cancellationToken: context.HttpContext.RequestAborted));
 
         return hasPermission > 0
             ? await next(context)
             : Results.Ok(ApiResult<string>.Fail(ApiCodes.Forbidden, "Insufficient permissions"));
     }
 }
