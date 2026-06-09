 using WeCms.Shared.Contracts;
 
 namespace WeCms.Modules.System.Dicts;
 
 public sealed class DictService(IDbConnectionFactory db)
 {
     public async Task<List<DictTypeItem>> GetTypesAsync(CancellationToken ct)
     { await using var c = await db.OpenAsync(ct); var r = await c.QueryAsync<DictTypeItem>(new CommandDefinition("SELECT id, code, name, status FROM sys_dict_type WHERE deleted_at IS NULL ORDER BY id", cancellationToken: ct)); return r.AsList(); }
 
     public async Task<List<DictValueItem>> GetValuesAsync(long typeId, CancellationToken ct)
     { await using var c = await db.OpenAsync(ct); var r = await c.QueryAsync<DictValueItem>(new CommandDefinition("SELECT id, type_id, code, name, value, sort, status FROM sys_dict_value WHERE type_id=@T AND deleted_at IS NULL ORDER BY sort", new { T = typeId }, cancellationToken: ct)); return r.AsList(); }
 
     public async Task<long> CreateTypeAsync(CreateDictTypeRequest req, CancellationToken ct)
     { await using var c = await db.OpenAsync(ct); return await c.ExecuteScalarAsync<long>(new CommandDefinition("INSERT INTO sys_dict_type (code,name,status,created_at,updated_at) VALUES (@C,@N,'active',@Now,@Now); SELECT LAST_INSERT_ID();", new { C = req.Code, N = req.Name, Now = DateTime.UtcNow }, cancellationToken: ct)); }
 
     public async Task<long> CreateValueAsync(CreateDictValueRequest req, CancellationToken ct)
     { await using var c = await db.OpenAsync(ct); return await c.ExecuteScalarAsync<long>(new CommandDefinition("INSERT INTO sys_dict_value (type_id,code,name,value,sort,status,created_at,updated_at) VALUES (@T,@C,@N,@V,@S,'active',@Now,@Now); SELECT LAST_INSERT_ID();", new { T = req.TypeId, C = req.Code, N = req.Name, req.Value, S = req.Sort, Now = DateTime.UtcNow }, cancellationToken: ct)); }
 
     public async Task DeleteTypeAsync(long id, CancellationToken ct)
     { await using var c = await db.OpenAsync(ct); await c.ExecuteAsync(new CommandDefinition("UPDATE sys_dict_type SET deleted_at=@Now WHERE id=@Id", new { Now = DateTime.UtcNow, Id = id }, cancellationToken: ct)); }
 
     public async Task DeleteValueAsync(long id, CancellationToken ct)
     { await using var c = await db.OpenAsync(ct); await c.ExecuteAsync(new CommandDefinition("UPDATE sys_dict_value SET deleted_at=@Now WHERE id=@Id", new { Now = DateTime.UtcNow, Id = id }, cancellationToken: ct)); }
 }
