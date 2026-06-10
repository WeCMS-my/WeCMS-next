using Dapper;
using WeCms.Shared.Contracts;

namespace WeCms.Modules.System.Dicts;

public sealed class DictService(IDbConnectionFactory db, IClock clock, IAuditWriter audit) : IDictService
{
    public async Task<(IReadOnlyList<DictTypeItem> Types, long Total)> GetTypesAsync(int page, int size, CancellationToken ct)
    {
        await using var c = await db.OpenAsync(ct);
        size = Math.Min(size, 100);
        var items = await c.QueryAsync<DictTypeItem>(new CommandDefinition(
            "SELECT id, code, name, status FROM sys_dict_type WHERE deleted_at IS NULL ORDER BY id LIMIT @L OFFSET @O",
            new { L = size, O = (page - 1) * size }, cancellationToken: ct));
        var total = await c.ExecuteScalarAsync<long>(new CommandDefinition(
            "SELECT COUNT(1) FROM sys_dict_type WHERE deleted_at IS NULL", cancellationToken: ct));
        return (items.AsList(), total);
    }

    public async Task<(IReadOnlyList<DictValueItem> Values, long Total)> GetValuesAsync(long typeId, int page, int size, CancellationToken ct)
    {
        await using var c = await db.OpenAsync(ct);
        size = Math.Min(size, 100);
        var items = await c.QueryAsync<DictValueItem>(new CommandDefinition(
            "SELECT id, type_id, code, name, value, sort, status FROM sys_dict_value WHERE type_id=@T AND deleted_at IS NULL ORDER BY sort LIMIT @L OFFSET @O",
            new { T = typeId, L = size, O = (page - 1) * size }, cancellationToken: ct));
        var total = await c.ExecuteScalarAsync<long>(new CommandDefinition(
            "SELECT COUNT(1) FROM sys_dict_value WHERE type_id=@T AND deleted_at IS NULL", new { T = typeId }, cancellationToken: ct));
        return (items.AsList(), total);
    }

    public async Task<long> CreateTypeAsync(CreateDictTypeRequest req, CancellationToken ct)
    { await using var c = await db.OpenAsync(ct); var id = await c.ExecuteScalarAsync<long>(new CommandDefinition("INSERT INTO sys_dict_type (code,name,status,created_at,updated_at) VALUES (@C,@N,'active',@Now,@Now); SELECT LAST_INSERT_ID();", new { C = req.Code, N = req.Name, Now = clock.UtcNow.DateTime }, cancellationToken: ct)); await audit.LogAsync("system", "dict:type:create", null, null, null, null, 200, "success", ct); return id; }

    public async Task<long> CreateValueAsync(CreateDictValueRequest req, CancellationToken ct)
    { await using var c = await db.OpenAsync(ct); var id = await c.ExecuteScalarAsync<long>(new CommandDefinition("INSERT INTO sys_dict_value (type_id,code,name,value,sort,status,created_at,updated_at) VALUES (@T,@C,@N,@V,@S,'active',@Now,@Now); SELECT LAST_INSERT_ID();", new { T = req.TypeId, C = req.Code, N = req.Name, req.Value, S = req.Sort, Now = clock.UtcNow.DateTime }, cancellationToken: ct)); await audit.LogAsync("system", "dict:value:create", null, null, null, null, 200, "success", ct); return id; }

    public async Task DeleteTypeAsync(long id, CancellationToken ct)
    { await using var c = await db.OpenAsync(ct); var affected = await c.ExecuteAsync(new CommandDefinition("UPDATE sys_dict_type SET deleted_at=@Now WHERE id=@Id", new { Now = clock.UtcNow.DateTime, Id = id }, cancellationToken: ct)); if (affected == 0) throw new InvalidOperationException("Dict type not found or already deleted"); await c.ExecuteAsync(new CommandDefinition("UPDATE sys_dict_value SET deleted_at=@Now WHERE type_id=@Id AND deleted_at IS NULL", new { Now = clock.UtcNow.DateTime, Id = id }, cancellationToken: ct)); await audit.LogAsync("system", "dict:type:delete", null, null, null, null, 200, "success", ct); }

    public async Task DeleteValueAsync(long id, CancellationToken ct)
    { await using var c = await db.OpenAsync(ct); var affected = await c.ExecuteAsync(new CommandDefinition("UPDATE sys_dict_value SET deleted_at=@Now WHERE id=@Id", new { Now = clock.UtcNow.DateTime, Id = id }, cancellationToken: ct)); if (affected == 0) throw new InvalidOperationException("Dict value not found or already deleted"); await audit.LogAsync("system", "dict:value:delete", null, null, null, null, 200, "success", ct); }
}
