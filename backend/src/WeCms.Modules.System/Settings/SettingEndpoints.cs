 using WeCms.Shared;
 
 namespace WeCms.Modules.System.Settings;
 
 public static class SettingEndpoints
 {
     public static RouteGroupBuilder MapSettingEndpoints(this RouteGroupBuilder group)
     {
         group.MapGet("/system/settings", GetAllAsync).RequirePermission("sys:setting:list");
         group.MapPut("/system/settings/{key}", UpdateAsync).RequirePermission("sys:setting:update");
         return group;
     }
     private static async Task<IResult> GetAllAsync(ISettingService svc, CancellationToken ct)
        => Results.Ok(ApiResult<List<SettingItem>>.Ok(await svc.GetAllAsync(ct)));
    private static async Task<IResult> UpdateAsync(string key, UpdateSettingRequest req, ISettingService svc, CancellationToken ct)
     { await svc.UpdateAsync(key, req.Value, ct); return Results.Ok(ApiResult<string>.Ok("saved")); }
 }
