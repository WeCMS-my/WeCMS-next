using SqlSugar;
using WeCms.Modules.System.Users;
using WeCms.Shared;

namespace WeCms.Persistence.Modules.System.Users;

public sealed class UserRepository : IUserRepository
{
    private readonly ISqlSugarClient _db;

    public UserRepository(ISqlSugarClient db)
    {
        _db = db;
    }

    public async Task<PagedResult<UserSummaryDto>> ListAsync(UserListCriteria criteria, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var where = "WHERE u.deleted_at IS NULL";
        var parameters = new List<SugarParameter>();
        if (!string.IsNullOrWhiteSpace(criteria.Keyword))
        {
            where += " AND (u.username LIKE @keyword OR u.display_name LIKE @keyword OR u.email LIKE @keyword OR u.phone LIKE @keyword)";
            parameters.Add(new SugarParameter("@keyword", $"%{criteria.Keyword}%"));
        }

        if (!string.IsNullOrWhiteSpace(criteria.Status))
        {
            where += " AND u.status = @status";
            parameters.Add(new SugarParameter("@status", criteria.Status));
        }

        if (criteria.DeptId is not null)
        {
            where += " AND u.dept_id = @deptId";
            parameters.Add(new SugarParameter("@deptId", criteria.DeptId.Value));
        }

        var total = Convert.ToInt64(await _db.Ado.GetScalarAsync(
            $"SELECT COUNT(1) FROM sys_user u {where}",
            parameters), global::System.Globalization.CultureInfo.InvariantCulture);

        var offset = (criteria.Page - 1) * criteria.PageSize;
        parameters.Add(new SugarParameter("@offset", offset));
        parameters.Add(new SugarParameter("@pageSize", criteria.PageSize));

        var rows = await _db.Ado.SqlQueryAsync<UserSummaryRow>(
            $"""
            SELECT u.id AS Id,
                   u.username AS Username,
                   u.display_name AS DisplayName,
                   u.email AS Email,
                   u.phone AS Phone,
                   u.dept_id AS DeptId,
                   u.status AS Status,
                   u.is_super_admin AS IsSuperAdmin,
                   u.last_login_at AS LastLoginAt,
                   u.created_at AS CreatedAt
            FROM sys_user u
            {where}
            ORDER BY u.id DESC
            LIMIT @pageSize OFFSET @offset
            """,
            parameters);

        return new PagedResult<UserSummaryDto>(
            rows.Select(row => row.ToDto()).ToArray(),
            criteria.Page,
            criteria.PageSize,
            total);
    }

    public async Task<UserDetailDto?> GetAsync(long id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var row = await _db.Ado.SqlQuerySingleAsync<UserDetailRow>(
            """
            SELECT u.id AS Id,
                   u.username AS Username,
                   u.display_name AS DisplayName,
                   u.email AS Email,
                   u.phone AS Phone,
                   u.dept_id AS DeptId,
                   u.status AS Status,
                   u.is_super_admin AS IsSuperAdmin,
                   u.permission_version AS PermissionVersion,
                   u.last_login_at AS LastLoginAt,
                   u.created_at AS CreatedAt,
                   u.updated_at AS UpdatedAt
            FROM sys_user u
            WHERE u.id = @id
              AND u.deleted_at IS NULL
            LIMIT 1
            """,
            new SugarParameter("@id", id));

        if (row is null)
        {
            return null;
        }

        var roles = await _db.Ado.SqlQueryAsync<long>(
            """
            SELECT role_id
            FROM sys_user_role
            WHERE user_id = @id
            ORDER BY role_id
            """,
            new SugarParameter("@id", id));
        var posts = await _db.Ado.SqlQueryAsync<long>(
            """
            SELECT post_id
            FROM sys_user_post
            WHERE user_id = @id
            ORDER BY post_id
            """,
            new SugarParameter("@id", id));

        return row.ToDto(roles, posts);
    }

    public Task<bool> UsernameExistsAsync(string username, long? exceptUserId, CancellationToken cancellationToken)
    {
        return ExistsAsync("username", username, exceptUserId, cancellationToken);
    }

    public Task<bool> EmailExistsAsync(string email, long? exceptUserId, CancellationToken cancellationToken)
    {
        return ExistsAsync("email", email, exceptUserId, cancellationToken);
    }

    public Task<bool> PhoneExistsAsync(string phone, long? exceptUserId, CancellationToken cancellationToken)
    {
        return ExistsAsync("phone", phone, exceptUserId, cancellationToken);
    }

    public async Task<bool> DeptExistsAsync(long deptId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Convert.ToInt32(await _db.Ado.GetScalarAsync(
            "SELECT COUNT(1) FROM sys_dept WHERE id = @deptId AND deleted_at IS NULL",
            new SugarParameter("@deptId", deptId)), global::System.Globalization.CultureInfo.InvariantCulture) == 1;
    }

    public Task<IReadOnlySet<long>> ExistingRoleIdsAsync(IReadOnlyList<long> roleIds, CancellationToken cancellationToken)
    {
        return ExistingIdsAsync("sys_role", roleIds, cancellationToken);
    }

    public Task<IReadOnlySet<long>> ExistingPostIdsAsync(IReadOnlyList<long> postIds, CancellationToken cancellationToken)
    {
        return ExistingIdsAsync("sys_post", postIds, cancellationToken);
    }

    public async Task<long> CreateAsync(UserCreateRecord record, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await _db.Ado.ExecuteCommandAsync(
            """
            INSERT INTO sys_user (username, display_name, email, phone, password_hash, status, is_super_admin, dept_id, must_change_password, security_stamp, permission_version, created_at, updated_at, deleted_at)
            VALUES (@username, @displayName, @email, @phone, @passwordHash, 'enabled', FALSE, @deptId, FALSE, @securityStamp, 0, @createdAt, @updatedAt, NULL)
            """,
            new SugarParameter("@username", record.Username),
            new SugarParameter("@displayName", record.DisplayName),
            new SugarParameter("@email", record.Email),
            new SugarParameter("@phone", record.Phone),
            new SugarParameter("@passwordHash", record.PasswordHash),
            new SugarParameter("@deptId", record.DeptId),
            new SugarParameter("@securityStamp", Guid.NewGuid().ToString("N")),
            new SugarParameter("@createdAt", record.Now.UtcDateTime),
            new SugarParameter("@updatedAt", record.Now.UtcDateTime));

        return Convert.ToInt64(await _db.Ado.GetScalarAsync("SELECT LAST_INSERT_ID()"), global::System.Globalization.CultureInfo.InvariantCulture);
    }

    public async Task UpdateAsync(UserUpdateRecord record, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await ExpectOneAsync(
            """
            UPDATE sys_user
            SET display_name = @displayName,
                email = @email,
                phone = @phone,
                dept_id = @deptId,
                updated_at = @updatedAt
            WHERE id = @id
              AND deleted_at IS NULL
            """,
            cancellationToken,
            new SugarParameter("@displayName", record.DisplayName),
            new SugarParameter("@email", record.Email),
            new SugarParameter("@phone", record.Phone),
            new SugarParameter("@deptId", record.DeptId),
            new SugarParameter("@updatedAt", record.Now.UtcDateTime),
            new SugarParameter("@id", record.Id));
    }

    public Task SoftDeleteAsync(long id, DateTimeOffset now, CancellationToken cancellationToken)
    {
        return ExpectOneAsync(
            """
            UPDATE sys_user
            SET deleted_at = @deletedAt,
                updated_at = @updatedAt
            WHERE id = @id
              AND deleted_at IS NULL
            """,
            cancellationToken,
            new SugarParameter("@deletedAt", now.UtcDateTime),
            new SugarParameter("@updatedAt", now.UtcDateTime),
            new SugarParameter("@id", id));
    }

    public Task SetStatusAsync(long id, string status, DateTimeOffset now, CancellationToken cancellationToken)
    {
        return ExpectOneAsync(
            """
            UPDATE sys_user
            SET status = @status,
                updated_at = @updatedAt
            WHERE id = @id
              AND deleted_at IS NULL
            """,
            cancellationToken,
            new SugarParameter("@status", status),
            new SugarParameter("@updatedAt", now.UtcDateTime),
            new SugarParameter("@id", id));
    }

    public Task ResetPasswordAsync(long id, string passwordHash, DateTimeOffset now, CancellationToken cancellationToken)
    {
        return ExpectOneAsync(
            """
            UPDATE sys_user
            SET password_hash = @passwordHash,
                security_stamp = @securityStamp,
                updated_at = @updatedAt
            WHERE id = @id
              AND deleted_at IS NULL
            """,
            cancellationToken,
            new SugarParameter("@passwordHash", passwordHash),
            new SugarParameter("@securityStamp", Guid.NewGuid().ToString("N")),
            new SugarParameter("@updatedAt", now.UtcDateTime),
            new SugarParameter("@id", id));
    }

    public async Task ReplaceRolesAsync(long id, IReadOnlyList<long> roleIds, DateTimeOffset now, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _db.Ado.BeginTran();
        try
        {
            await _db.Ado.ExecuteCommandAsync("DELETE FROM sys_user_role WHERE user_id = @id", new SugarParameter("@id", id));
            foreach (var roleId in roleIds)
            {
                await _db.Ado.ExecuteCommandAsync(
                    "INSERT INTO sys_user_role (user_id, role_id, created_at) VALUES (@id, @roleId, @createdAt)",
                    new SugarParameter("@id", id),
                    new SugarParameter("@roleId", roleId),
                    new SugarParameter("@createdAt", now.UtcDateTime));
            }

            await BumpPermissionVersionAsync(id, now);
            _db.Ado.CommitTran();
        }
        catch
        {
            _db.Ado.RollbackTran();
            throw;
        }
    }

    public async Task ReplacePostsAsync(long id, IReadOnlyList<long> postIds, DateTimeOffset now, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _db.Ado.BeginTran();
        try
        {
            await _db.Ado.ExecuteCommandAsync("DELETE FROM sys_user_post WHERE user_id = @id", new SugarParameter("@id", id));
            foreach (var postId in postIds)
            {
                await _db.Ado.ExecuteCommandAsync(
                    "INSERT INTO sys_user_post (user_id, post_id, created_at) VALUES (@id, @postId, @createdAt)",
                    new SugarParameter("@id", id),
                    new SugarParameter("@postId", postId),
                    new SugarParameter("@createdAt", now.UtcDateTime));
            }

            _db.Ado.CommitTran();
        }
        catch
        {
            _db.Ado.RollbackTran();
            throw;
        }
    }

    public async Task<int> CountActiveSuperAdminsExceptAsync(long? exceptUserId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var sql = """
            SELECT COUNT(1)
            FROM sys_user
            WHERE is_super_admin = TRUE
              AND status = 'enabled'
              AND deleted_at IS NULL
            """;
        var parameters = new List<SugarParameter>();
        if (exceptUserId is not null)
        {
            sql += " AND id <> @exceptUserId";
            parameters.Add(new SugarParameter("@exceptUserId", exceptUserId.Value));
        }

        return Convert.ToInt32(await _db.Ado.GetScalarAsync(sql, parameters), global::System.Globalization.CultureInfo.InvariantCulture);
    }

    public Task RecordAuditAsync(UserAuditRecord record, CancellationToken cancellationToken)
    {
        return ExpectOneAsync(
            """
            INSERT INTO sys_audit_log (user_id, username, module, resource, action, target_id, request_method, request_path, ip_address, user_agent, trace_id, result, detail, created_at)
            VALUES (@userId, @username, 'system', 'user', @action, @targetId, @requestMethod, @requestPath, @ipAddress, @userAgent, @traceId, @result, @detail, @createdAt)
            """,
            cancellationToken,
            new SugarParameter("@userId", record.ActorUserId),
            new SugarParameter("@username", record.ActorUsername),
            new SugarParameter("@action", record.Action),
            new SugarParameter("@targetId", record.TargetUserId.ToString(global::System.Globalization.CultureInfo.InvariantCulture)),
            new SugarParameter("@requestMethod", string.Empty),
            new SugarParameter("@requestPath", "/api/v1/system/users"),
            new SugarParameter("@ipAddress", record.Ip),
            new SugarParameter("@userAgent", record.UserAgent),
            new SugarParameter("@traceId", record.TraceId),
            new SugarParameter("@result", record.Result),
            new SugarParameter("@detail", record.Detail),
            new SugarParameter("@createdAt", record.CreatedAt.UtcDateTime));
    }

    private async Task<bool> ExistsAsync(string column, string value, long? exceptUserId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var sql = $"SELECT COUNT(1) FROM sys_user WHERE {column} = @value AND deleted_at IS NULL";
        var parameters = new List<SugarParameter> { new("@value", value) };
        if (exceptUserId is not null)
        {
            sql += " AND id <> @exceptUserId";
            parameters.Add(new SugarParameter("@exceptUserId", exceptUserId.Value));
        }

        return Convert.ToInt32(await _db.Ado.GetScalarAsync(sql, parameters), global::System.Globalization.CultureInfo.InvariantCulture) > 0;
    }

    private async Task<IReadOnlySet<long>> ExistingIdsAsync(string table, IReadOnlyList<long> ids, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (ids.Count == 0)
        {
            return new HashSet<long>();
        }

        var parameters = ids.Select((id, index) => new SugarParameter($"@id{index}", id)).ToArray();
        var placeholders = string.Join(", ", parameters.Select(parameter => parameter.ParameterName));
        var rows = await _db.Ado.SqlQueryAsync<long>($"SELECT id FROM {table} WHERE id IN ({placeholders})", parameters);

        return rows.ToHashSet();
    }

    private Task BumpPermissionVersionAsync(long id, DateTimeOffset now)
    {
        return _db.Ado.ExecuteCommandAsync(
            """
            UPDATE sys_user
            SET permission_version = permission_version + 1,
                updated_at = @updatedAt
            WHERE id = @id
            """,
            new SugarParameter("@updatedAt", now.UtcDateTime),
            new SugarParameter("@id", id));
    }

    private async Task ExpectOneAsync(string sql, CancellationToken cancellationToken, params SugarParameter[] parameters)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var rows = await _db.Ado.ExecuteCommandAsync(sql, parameters);
        if (rows != 1)
        {
            throw new InvalidOperationException($"Expected one affected row, got {rows}.");
        }
    }

    private class UserSummaryRow
    {
        public long Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public long? DeptId { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsSuperAdmin { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public DateTime CreatedAt { get; set; }

        public UserSummaryDto ToDto()
        {
            return new UserSummaryDto(Id, Username, DisplayName, Email, Phone, DeptId, Status, IsSuperAdmin, ToOffset(LastLoginAt), ToOffset(CreatedAt)!.Value);
        }
    }

    private sealed class UserDetailRow : UserSummaryRow
    {
        public long PermissionVersion { get; set; }
        public DateTime UpdatedAt { get; set; }

        public UserDetailDto ToDto(IReadOnlyList<long> roleIds, IReadOnlyList<long> postIds)
        {
            return new UserDetailDto(
                Id,
                Username,
                DisplayName,
                Email,
                Phone,
                DeptId,
                Status,
                IsSuperAdmin,
                PermissionVersion,
                ToOffset(LastLoginAt),
                roleIds,
                postIds,
                ToOffset(CreatedAt)!.Value,
                ToOffset(UpdatedAt)!.Value);
        }
    }

    private static DateTimeOffset? ToOffset(DateTime? value)
    {
        return value is null ? null : new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc));
    }
}
