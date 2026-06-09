 using WeCms.Shared;
 
 namespace WeCms.Modules.System.Dicts;
 
 public static class DictEndpoints
 {
     public static RouteGroupBuilder MapDictEndpoints(this RouteGroupBuilder group)
     {
         group.MapGet("/system/dicts/types", GetTypesAsync).RequirePermission("sys:dict:list");
         group.MapPost("/system/dicts/types", CreateTypeAsync).RequirePermission("sys:dict:create");
         group.MapDelete("/system/dicts/types/{id:long}", DeleteTypeAsync).RequirePermission("sys:dict:delete");
         group.MapGet("/system/dicts/types/{typeId:long}/values", GetValuesAsync).RequirePermission("sys:dict:list");
         group.MapPost("/system/dicts/values", CreateValueAsync).RequirePermission("sys:dict:create");
         group.MapDelete("/system/dicts/values/{id:long}", DeleteValueAsync).RequirePermission("sys:dict:delete");
         return group;
     }
     private static async Task<IResult> GetTypesAsync(IDictService svc, CancellationToken ct) => Results.Ok(ApiResult<List<DictTypeItem>>.Ok(await svc.GetTypesAsync(ct)));
    private static async Task<IResult> CreateTypeAsync(CreateDictTypeRequest req, IDictService svc, CancellationToken ct) => Results.Ok(ApiResult<IdResponse>.Ok(new IdResponse(await svc.CreateTypeAsync(req, ct))));
    private static async Task<IResult> DeleteTypeAsync(long id, IDictService svc, CancellationToken ct) { await svc.DeleteTypeAsync(id, ct); return Results.Ok(ApiResult<string>.Ok("deleted")); }
    private static async Task<IResult> GetValuesAsync(long typeId, IDictService svc, CancellationToken ct) => Results.Ok(ApiResult<List<DictValueItem>>.Ok(await svc.GetValuesAsync(typeId, ct)));
    private static async Task<IResult> CreateValueAsync(CreateDictValueRequest req, IDictService svc, CancellationToken ct) => Results.Ok(ApiResult<IdResponse>.Ok(new IdResponse(await svc.CreateValueAsync(req, ct))));
    private static async Task<IResult> DeleteValueAsync(long id, IDictService svc, CancellationToken ct) { await svc.DeleteValueAsync(id, ct); return Results.Ok(ApiResult<string>.Ok("deleted")); }
 }
