 using WeCms.Shared.Contracts;
 
 namespace WeCms.Modules.System.I18n;
 
 public sealed class I18nService(IDbConnectionFactory db) : II18nService
 {
     public async Task<List<I18nMessageItem>> ListAsync(string? locale, string? key, CancellationToken ct)
     { await using var c = await db.OpenAsync(ct); var items = await c.QueryAsync<I18nMessageItem>(new CommandDefinition("SELECT id, locale, message_key, message_value, remark FROM sys_i18n_message WHERE deleted_at IS NULL AND (@L IS NULL OR locale=@L) AND (@K IS NULL OR message_key LIKE CONCAT('%',@K,'%')) ORDER BY locale, message_key LIMIT 500", new { L = locale, K = key }, cancellationToken: ct)); return items.AsList(); }
     public async Task<long> CreateAsync(CreateI18nRequest req, CancellationToken ct)
     { await using var c = await db.OpenAsync(ct); return await c.ExecuteScalarAsync<long>(new CommandDefinition("INSERT INTO sys_i18n_message (locale,message_key,message_value,remark,created_at,updated_at) VALUES (@L,@K,@V,@R,@N,@N); SELECT LAST_INSERT_ID();", new { L = req.Locale, K = req.MessageKey, V = req.MessageValue, req.Remark, N = DateTime.UtcNow }, cancellationToken: ct)); }
     public async Task UpdateAsync(long id, UpdateI18nRequest req, CancellationToken ct)
     { await using var c = await db.OpenAsync(ct); await c.ExecuteAsync(new CommandDefinition("UPDATE sys_i18n_message SET message_value=COALESCE(@V,message_value), remark=COALESCE(@R,remark), updated_at=@N WHERE id=@Id", new { req.MessageValue, req.Remark, N = DateTime.UtcNow, Id = id }, cancellationToken: ct)); }
     public async Task DeleteAsync(long id, CancellationToken ct)
     { await using var c = await db.OpenAsync(ct); await c.ExecuteAsync(new CommandDefinition("UPDATE sys_i18n_message SET deleted_at=@N WHERE id=@Id", new { N = DateTime.UtcNow, Id = id }, cancellationToken: ct)); }
 }
