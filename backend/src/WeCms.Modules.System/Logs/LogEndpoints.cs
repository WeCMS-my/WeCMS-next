 using WeCms.Shared;
 
 namespace WeCms.Modules.System.Logs;
 
 public static class LogEndpoints
 {
     public static RouteGroupBuilder MapLogEndpoints(this RouteGroupBuilder group)
     {
         group.MapGet("/system/logs/login", GetLoginLogsAsync).RequirePermission("sys:log:login:list");
         group.MapGet("/system/logs/audit", GetAuditLogsAsync).RequirePermission("sys:log:audit:list");
         return group;
     }
     private static async Task<IResult> GetLoginLogsAsync(HttpContext ctx, LogService svc, CancellationToken ct)
     { var p = int.TryParse(ctx.Request.Query["page"], out var pp) ? pp : 1; var s = int.TryParse(ctx.Request.Query["pageSize"], out var ps) ? ps : 20; var status = ctx.Request.Query["status"].FirstOrDefault(); var (items, total) = await svc.GetLoginLogsAsync(p, s, status, ct); return Results.Ok(ApiResult<object>.Ok(new { records = items, page = p, pageSize = s, total })); }
     private static async Task<IResult> GetAuditLogsAsync(HttpContext ctx, LogService svc, CancellationToken ct)
     { var p = int.TryParse(ctx.Request.Query["page"], out var pp) ? pp : 1; var s = int.TryParse(ctx.Request.Query["pageSize"], out var ps) ? ps : 20; var mod = ctx.Request.Query["module"].FirstOrDefault(); var (items, total) = await svc.GetAuditLogsAsync(p, s, mod, ct); return Results.Ok(ApiResult<object>.Ok(new { records = items, page = p, pageSize = s, total })); }
 }
