 using WeCms.Shared.Contracts;
 
 namespace WeCms.Modules.System.Dicts;
 
 public sealed class DictService(IDbConnectionFactory db, IClock clock, IAuditWriter audit) : IDictService
{
    public async Task<List<DictTypeItem>> GetTypesAsync(CancellationToken ct)
     { await using var c = await db.OpenAsync(ct); var r = await c.QueryAsync<DictTypeItem>(new CommandDefinition("SELECT id, code, name, status FROM sys_dict_type WHERE deleted_at IS NULL ORDER BY id", cancellationToken: ct)); return r.AsList(); }
 
     public async Task<List<DictValueItem>> GetValuesAsync(long typeId, CancellationToken ct)
     { await using var c = await db.OpenAsync(ct); var r = await c.QueryAsync<DictValueItem>(new CommandDefinition("SELECT id, type_id, code, name, value, sort, status FROM sys_dict_value WHERE type_id=@T AND deleted_at IS NULL ORDER BY sort", new { T = typeId }, cancellationToken: ct)); return r.AsList(); }
 
     public async Task<long> CreateTypeAsync(CreateDictTypeRequest req, CancellationToken ct)
    { await using var c = await db.OpenAsync(ct); var id = await c.ExecuteScalarAsync<long>(new CommandDefinition("INSERT INTO sys_dict_type (code,name,status,created_at,updated_at) VALUES (@C,@N,'active',@Now,@Now); SELECT LAST_INSERT_ID();", new { C = req.Code, N = req.Name, Now = clock.UtcNow.DateTime }, cancellationToken: ct)); await audit.LogAsync("system", "dict:type:create", null, null, null, null, 200, "success", ct); return id; }

    public async Task<long> CreateValueAsync(CreateDictValueRequest req, CancellationToken ct)
    { await using var c = await db.OpenAsync(ct); var id = await c.ExecuteScalarAsync<long>(new CommandDefinition("INSERT INTO sys_dict_value (type_id,code,name,value,sort,status,created_at,updated_at) VALUES (@T,@C,@N,@V,@S,'active',@Now,@Now); SELECT LAST_INSERT_ID();", new { T = req.TypeId, C = req.Code, N = req.Name, req.Value, S = req.Sort, Now = clock.UtcNow.DateTime }, cancellationToken: ct)); await audit.LogAsync("system", "dict:value:create", null, null, null, null, 200, "success", ct); return id; }

    public async Task DeleteTypeAsync(long id, CancellationToken ct)
    { await using var c = await db.OpenAsync(ct); await c.ExecuteAsync(new CommandDefinition("UPDATE sys_dict_type SET deleted_at=@Now WHERE id=@Id", new { Now = clock.UtcNow.DateTime, Id = id }, cancellationToken: ct)); await c.ExecuteAsync(new CommandDefinition("UPDATE sys_dict_value SET deleted_at=@Now WHERE type_id=@Id AND deleted_at IS NULL", new { Now = clock.UtcNow.DateTime, Id = id }, cancellationToken: ct)); await audit.LogAsync("system", "dict:type:delete", null, null, null, null, 200, "success", ct); }

    public async Task DeleteValueAsync(long id, CancellationToken ct)
    { await using var c = await db.OpenAsync(ct); await c.ExecuteAsync(new CommandDefinition("UPDATE sys_dict_value SET deleted_at=@Now WHERE id=@Id", new { Now = clock.UtcNow.DateTime, Id = id }, cancellationToken: ct)); await audit.LogAsync("system", "dict:value:delete", null, null, null, null, 200, "success", ct); }
 }
