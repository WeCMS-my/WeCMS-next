 using WeCms.Shared;
 
 namespace WeCms.Modules.System.Security;
 
 public static class SecurityEndpoints
 {
     public static RouteGroupBuilder MapSecurityEndpoints(this RouteGroupBuilder group)
     {
         group.MapGet("/system/security/events", ListEventsAsync).RequirePermission("sys:security:event:list");
         return group;
     }
     private static async Task<IResult> ListEventsAsync(HttpContext ctx, ISecurityService svc, CancellationToken ct)
     { var p = int.TryParse(ctx.Request.Query["page"], out var pp) ? pp : 1; var s = int.TryParse(ctx.Request.Query["pageSize"], out var ps) ? ps : 20; var t = ctx.Request.Query["type"].FirstOrDefault(); var (items, total) = await svc.ListEventsAsync(p, s, t, ct); return Results.Ok(ApiResult<PagedResult<SecurityEventItem>>.Ok(new PagedResult<SecurityEventItem>(items, p, s, total))); }
 }
