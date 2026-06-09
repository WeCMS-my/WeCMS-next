using WeCms.Shared.Contracts;

namespace WeCms.Modules.System.Users;

public sealed class UserService(IPasswordHasher hasher, IDbConnectionFactory db, IClock clock, IAuditWriter audit) : IUserService
{
    private static readonly HashSet<string> Sorts = new(StringComparer.OrdinalIgnoreCase) { "id", "username", "display_name", "created_at", "status" };
    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase) { "active", "disabled" };

    public async Task<(IReadOnlyList<UserListItem> Items, long Total)> ListAsync(UserQueryParams q, CancellationToken ct)
    {
        await using var conn = await db.OpenAsync(ct);
        var col = Sorts.Contains(q.SortBy ?? "") ? q.SortBy! : "id";
        var dir = q.SortDesc ? "DESC" : "ASC";
        var where = "deleted_at IS NULL AND (@K IS NULL OR username LIKE CONCAT('%',@K,'%') OR display_name LIKE CONCAT('%',@K,'%')) AND (@S IS NULL OR status=@S)";
        var items = await conn.QueryAsync<UserListItem>(new CommandDefinition($"SELECT id, username, display_name, email, status, is_super_admin, created_at FROM sys_user WHERE {where} ORDER BY {col} {dir} LIMIT @L OFFSET @O", new { K = q.Keyword, S = q.Status, L = Math.Min(q.PageSize, 100), O = (q.Page - 1) * q.PageSize }, cancellationToken: ct));
        var total = await conn.ExecuteScalarAsync<long>(new CommandDefinition($"SELECT COUNT(1) FROM sys_user WHERE {where}", new { K = q.Keyword, S = q.Status }, cancellationToken: ct));
        return (items.AsList(), total);
    }

    public async Task<UserDetail?> GetByIdAsync(long id, CancellationToken ct)
    { await using var conn = await db.OpenAsync(ct); return await conn.QueryFirstOrDefaultAsync<UserDetail>(new CommandDefinition("SELECT id, username, display_name, email, phone, avatar_file_id, status, is_super_admin, two_factor_enabled, last_login_at, last_login_ip, created_at, updated_at FROM sys_user WHERE id=@Id AND deleted_at IS NULL", new { Id = id }, cancellationToken: ct)); }

    public async Task<long> CreateAsync(CreateUserRequest req, long operatorId, CancellationToken ct)
    {
        await using var conn = await db.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        if (await conn.ExecuteScalarAsync<int>(new CommandDefinition("SELECT COUNT(1) FROM sys_user WHERE username=@U AND deleted_at IS NULL", new { U = req.Username }, cancellationToken: ct)) > 0) throw new InvalidOperationException("Username exists");
        var hash = hasher.Hash(req.Password);
        var id = await conn.ExecuteScalarAsync<long>(new CommandDefinition("INSERT INTO sys_user (username,display_name,email,phone,password_hash,password_hash_algorithm,status,security_stamp,permission_version,created_at,updated_at) VALUES (@U,@D,@E,@P,@H,'pbkdf2-sha256','active',@SS,1,@N,@N); SELECT LAST_INSERT_ID();", new { U = req.Username, D = req.DisplayName ?? req.Username, req.Email, req.Phone, H = hash, SS = Guid.NewGuid().ToString("N"), N = clock.UtcNow.DateTime }, cancellationToken: ct));
        if (req.RoleIds is { Length: > 0 }) await conn.ExecuteAsync(new CommandDefinition("INSERT INTO sys_user_role (user_id,role_id,created_at) VALUES (@Uid,@Rid,@Now)", req.RoleIds.Select(r => new { Uid = id, Rid = r, Now = clock.UtcNow.DateTime }), cancellationToken: ct));
        await tx.CommitAsync(ct);
        return id;
    }

    public async Task UpdateAsync(long id, UpdateUserRequest req, long operatorId, CancellationToken ct)
    { await using var c = await db.OpenAsync(ct); await using var tx = await c.BeginTransactionAsync(ct); var r = await c.QueryFirstOrDefaultAsync<UserRow>(new CommandDefinition("SELECT status, is_super_admin FROM sys_user WHERE id=@Id AND deleted_at IS NULL", new { Id = id }, cancellationToken: ct)); if (r is null) { await tx.RollbackAsync(ct); throw new InvalidOperationException("User not found"); } if (r.IsSuper != 0 && operatorId != id) { await tx.RollbackAsync(ct); throw new UnauthorizedAccessException("Cannot modify super admin"); } if (req.Status is not null && !AllowedStatuses.Contains(req.Status)) throw new InvalidOperationException("Invalid status"); await c.ExecuteAsync(new CommandDefinition("UPDATE sys_user SET display_name=COALESCE(@D,display_name), email=COALESCE(@E,email), phone=COALESCE(@P,phone), status=COALESCE(@S,status), updated_at=@N WHERE id=@Id", new { req.DisplayName, req.Email, req.Phone, req.Status, N = clock.UtcNow.DateTime, Id = id }, cancellationToken: ct)); if (req.RoleIds is not null) { await c.ExecuteAsync(new CommandDefinition("DELETE FROM sys_user_role WHERE user_id=@Id", new { Id = id }, cancellationToken: ct)); if (req.RoleIds.Length > 0) await c.ExecuteAsync(new CommandDefinition("INSERT INTO sys_user_role (user_id,role_id,created_at) VALUES (@Uid,@Rid,@Now)", req.RoleIds.Select(x => new { Uid = id, Rid = x, Now = clock.UtcNow.DateTime }), cancellationToken: ct)); await c.ExecuteAsync(new CommandDefinition("UPDATE sys_user SET permission_version = permission_version + 1 WHERE id = @Id", new { Id = id }, cancellationToken: ct)); } await tx.CommitAsync(ct); await audit.LogAsync("system", "user:update", operatorId, null, null, null, 200, "success", ct); }

    public async Task DeleteAsync(long id, long operatorId, CancellationToken ct)
    { if (id == operatorId) throw new InvalidOperationException("Cannot delete yourself"); await using var c = await db.OpenAsync(ct); var isSuper = await c.ExecuteScalarAsync<int>(new CommandDefinition("SELECT is_super_admin FROM sys_user WHERE id=@Id", new { Id = id }, cancellationToken: ct)); if (isSuper != 0) { var activeSuperCount = await c.ExecuteScalarAsync<int>(new CommandDefinition("SELECT COUNT(1) FROM sys_user WHERE is_super_admin=1 AND status='active' AND deleted_at IS NULL", cancellationToken: ct)); if (activeSuperCount <= 1) throw new InvalidOperationException("Cannot remove the last super admin"); } await c.ExecuteAsync(new CommandDefinition("UPDATE sys_user SET deleted_at=@N, deleted_by=@Op WHERE id=@Id AND deleted_at IS NULL", new { N = clock.UtcNow.DateTime, Op = operatorId, Id = id }, cancellationToken: ct)); await audit.LogAsync("system", "user:delete", operatorId, null, null, null, 200, "success", ct); }

    public async Task SetStatusAsync(long id, string status, long operatorId, CancellationToken ct)
    { if (!AllowedStatuses.Contains(status)) throw new InvalidOperationException("Invalid status"); if (id == operatorId && status != "active") throw new InvalidOperationException("Cannot disable yourself"); await using var c = await db.OpenAsync(ct); var isSuper = await c.ExecuteScalarAsync<int>(new CommandDefinition("SELECT is_super_admin FROM sys_user WHERE id=@Id", new { Id = id }, cancellationToken: ct)); if (isSuper != 0 && status != "active") { var activeSuperCount = await c.ExecuteScalarAsync<int>(new CommandDefinition("SELECT COUNT(1) FROM sys_user WHERE is_super_admin=1 AND status='active' AND deleted_at IS NULL", cancellationToken: ct)); if (activeSuperCount <= 1) throw new InvalidOperationException("Cannot remove the last super admin"); } await c.ExecuteAsync(new CommandDefinition("UPDATE sys_user SET status=@S, updated_at=@N WHERE id=@Id", new { S = status, N = clock.UtcNow.DateTime, Id = id }, cancellationToken: ct)); if (status != "active") { await c.ExecuteAsync(new CommandDefinition("UPDATE sys_refresh_token SET revoked_at=@N WHERE user_id=@Id AND revoked_at IS NULL", new { N = clock.UtcNow.DateTime, Id = id }, cancellationToken: ct)); } await audit.LogAsync("system", "user:status", operatorId, null, null, null, 200, "success", ct); }

    private sealed record UserRow(string Status, int IsSuper);
}
