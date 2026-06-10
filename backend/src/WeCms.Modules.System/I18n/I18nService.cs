using Dapper;
using WeCms.Shared.Contracts;

namespace WeCms.Modules.System.I18n;

public sealed class I18nService(IDbConnectionFactory db, IClock clock, IAuditWriter audit) : II18nService
{
    public async Task<(IReadOnlyList<I18nMessageItem> Items, long Total)> ListAsync(string? locale, string? key, int page, int size, CancellationToken ct)
    {
        await using var c = await db.OpenAsync(ct);
        size = Math.Min(size, 100);
        var items = await c.QueryAsync<I18nMessageItem>(new CommandDefinition(
            "SELECT id, locale, message_key, message_value, remark FROM sys_i18n_message WHERE deleted_at IS NULL AND (@L IS NULL OR locale=@L) AND (@K IS NULL OR message_key LIKE CONCAT('%',@K,'%')) ORDER BY locale, message_key LIMIT @S OFFSET @O",
            new { L = locale, K = key, S = size, O = (page - 1) * size }, cancellationToken: ct));
        var total = await c.ExecuteScalarAsync<long>(new CommandDefinition(
            "SELECT COUNT(1) FROM sys_i18n_message WHERE deleted_at IS NULL AND (@L IS NULL OR locale=@L) AND (@K IS NULL OR message_key LIKE CONCAT('%',@K,'%'))",
            new { L = locale, K = key }, cancellationToken: ct));
        return (items.AsList(), total);
    }
    public async Task<long> CreateAsync(CreateI18nRequest req, CancellationToken ct)
    { await using var c = await db.OpenAsync(ct); var id = await c.ExecuteScalarAsync<long>(new CommandDefinition("INSERT INTO sys_i18n_message (locale,message_key,message_value,remark,created_at,updated_at) VALUES (@L,@K,@V,@R,@N,@N); SELECT LAST_INSERT_ID();", new { L = req.Locale, K = req.MessageKey, V = req.MessageValue, req.Remark, N = clock.UtcNow.DateTime }, cancellationToken: ct)); await audit.LogAsync("system", "i18n:create", null, null, null, null, 200, "success", ct); return id; }
    public async Task UpdateAsync(long id, UpdateI18nRequest req, CancellationToken ct)
    { await using var c = await db.OpenAsync(ct); var affected = await c.ExecuteAsync(new CommandDefinition("UPDATE sys_i18n_message SET message_value=COALESCE(@V,message_value), remark=COALESCE(@R,remark), updated_at=@N WHERE id=@Id", new { req.MessageValue, req.Remark, N = clock.UtcNow.DateTime, Id = id }, cancellationToken: ct)); if (affected == 0) throw new InvalidOperationException("I18n message not found or already modified"); await audit.LogAsync("system", "i18n:update", null, null, null, null, 200, "success", ct); }
    public async Task DeleteAsync(long id, CancellationToken ct)
    { await using var c = await db.OpenAsync(ct); var affected = await c.ExecuteAsync(new CommandDefinition("UPDATE sys_i18n_message SET deleted_at=@N WHERE id=@Id", new { N = clock.UtcNow.DateTime, Id = id }, cancellationToken: ct)); if (affected == 0) throw new InvalidOperationException("I18n message not found or already deleted"); await audit.LogAsync("system", "i18n:delete", null, null, null, null, 200, "success", ct); }
}
