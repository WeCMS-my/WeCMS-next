using SqlSugar;
using WeCms.Shared;
using WeCms.Shared.Id;
using WeCms.Shared.Security;
using WeCms.Data.SqlSugar;

namespace WeCms.Modules.Identity.SqlSugar.Repositories;

public sealed partial class UserRepository : IUserRepository
{
    private readonly ISqlSugarClient _db;
    private readonly ISecurityEventClassifier _securityEventClassifier;
    private readonly IIdGenerator _idGenerator;

    public UserRepository(ISqlSugarClient db, ISecurityEventClassifier securityEventClassifier, IIdGenerator idGenerator)
    {
        _db = db;
        _securityEventClassifier = securityEventClassifier;
        _idGenerator = idGenerator;
    }

    public async Task<PagedResult<UserSummaryDto>> ListAsync(UserListCriteria criteria, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // P3 raw-sql-reviewed: PredicateBuilder supplies the sys_user soft-delete predicate.
        var userSoftDelete = SoftDeleteSqlPredicateBuilder.Build("u");
        var where = $"WHERE {userSoftDelete.Sql}";
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

        var totalSql = $"SELECT COUNT(1) FROM sys_user u {where}";
        RawSqlFilterGuard.RequireDeletedAtFilter(totalSql, nameof(ListAsync));
        var total = Convert.ToInt64(await _db.Ado.GetScalarAsync(totalSql, parameters), global::System.Globalization.CultureInfo.InvariantCulture);

        var offset = (criteria.Page - 1) * criteria.PageSize;
        parameters.Add(new SugarParameter("@offset", offset));
        parameters.Add(new SugarParameter("@pageSize", criteria.PageSize));

        var rowsSql =
            $"""
            SELECT u.id AS Id,
                   u.username AS Username,
                   u.display_name AS DisplayName,
                   u.email AS Email,
                   u.phone AS Phone,
                   u.dept_id AS DeptId,
                   u.status AS Status,
                   u.last_login_at AS LastLoginAt,
                   u.created_at AS CreatedAt
            FROM sys_user u
            {where}
            ORDER BY u.id DESC
            LIMIT @pageSize OFFSET @offset
            """;
        RawSqlFilterGuard.RequireDeletedAtFilter(rowsSql, nameof(ListAsync));
        var rows = await _db.Ado.SqlQueryAsync<UserSummaryRow>(
            rowsSql,
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
        // P3 raw-sql-reviewed: PredicateBuilder supplies the sys_user soft-delete predicate.
        var userSoftDelete = SoftDeleteSqlPredicateBuilder.Build("u");

        var rowSql =
            $"""
            SELECT u.id AS Id,
                   u.username AS Username,
                   u.display_name AS DisplayName,
                   u.email AS Email,
                   u.phone AS Phone,
                   u.dept_id AS DeptId,
                   u.status AS Status,
                   u.permission_version AS PermissionVersion,
                   u.last_login_at AS LastLoginAt,
                   u.created_at AS CreatedAt,
                   u.updated_at AS UpdatedAt
            FROM sys_user u
            WHERE u.id = @id
              AND {userSoftDelete.Sql}
            LIMIT 1
            """;
        RawSqlFilterGuard.RequireDeletedAtFilter(rowSql, nameof(GetAsync));
        var row = await _db.Ado.SqlQuerySingleAsync<UserDetailRow>(
            rowSql,
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
        var positions = await _db.Ado.SqlQueryAsync<long>(
            """
            SELECT position_id
            FROM sys_user_position
            WHERE user_id = @id
            ORDER BY position_id
            """,
            new SugarParameter("@id", id));

        return row.ToDto(roles, positions);
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

    public Task<IReadOnlySet<long>> ExistingRoleIdsAsync(IReadOnlyList<long> roleIds, CancellationToken cancellationToken)
    {
        return ExistingActiveRoleIdsAsync(roleIds, cancellationToken);
    }

    public async Task<long> CreateAsync(UserCreateRecord record, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await ExpectOneAsync(
            """
            INSERT INTO sys_user (username, display_name, email, phone, password_hash, status, dept_id, must_change_password, security_stamp, permission_version, created_at, updated_at, deleted_at)
            VALUES (@username, @displayName, @email, @phone, @passwordHash, 'enabled', @deptId, FALSE, @securityStamp, 0, @createdAt, @updatedAt, NULL)
            """,
            cancellationToken,
            new SugarParameter("@username", record.Username),
            new SugarParameter("@displayName", record.DisplayName),
            new SugarParameter("@email", record.Email),
            new SugarParameter("@phone", record.Phone),
            new SugarParameter("@passwordHash", record.PasswordHash),
            new SugarParameter("@deptId", record.DeptId),
            new SugarParameter("@securityStamp", _idGenerator.NewId()),
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
            requireDeletedAtFilter: true,
            new SugarParameter("@displayName", record.DisplayName),
            new SugarParameter("@email", record.Email),
            new SugarParameter("@phone", record.Phone),
            new SugarParameter("@deptId", record.DeptId),
            new SugarParameter("@updatedAt", record.Now.UtcDateTime),
            new SugarParameter("@id", record.Id));
    }

    public async Task SoftDeleteAsync(long id, DateTimeOffset now, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await ExpectOneAsync(
            """
            UPDATE sys_user
            SET deleted_at = @deletedAt,
                status = 'disabled',
                updated_at = @updatedAt
            WHERE id = @id
              AND deleted_at IS NULL
            """,
            cancellationToken,
            requireDeletedAtFilter: true,
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
            requireDeletedAtFilter: true,
            new SugarParameter("@status", status),
            new SugarParameter("@updatedAt", now.UtcDateTime),
            new SugarParameter("@id", id));
    }

    public Task RevokeUserRefreshTokensAsync(long userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return _db.Ado.ExecuteCommandAsync(
            """
            UPDATE sys_refresh_token
            SET revoked_at = @revokedAt
            WHERE user_id = @userId
              AND revoked_at IS NULL
            """,
            new SugarParameter("@revokedAt", now.UtcDateTime),
            new SugarParameter("@userId", userId));
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
            requireDeletedAtFilter: true,
            new SugarParameter("@passwordHash", passwordHash),
            new SugarParameter("@securityStamp", _idGenerator.NewId()),
            new SugarParameter("@updatedAt", now.UtcDateTime),
            new SugarParameter("@id", id));
    }

    public async Task ReplaceRolesAsync(long id, IReadOnlyList<long> roleIds, DateTimeOffset now, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await EnsureActiveUserExistsAsync(id, cancellationToken);

        await ExecuteOptionalAsync(
            "DELETE FROM sys_user_role WHERE user_id = @id",
            cancellationToken,
            new SugarParameter("@id", id));
        foreach (var roleId in roleIds)
        {
            await ExpectOneAsync(
                "INSERT INTO sys_user_role (user_id, role_id, created_at) VALUES (@id, @roleId, @createdAt)",
            cancellationToken,
            requireDeletedAtFilter: false,
            new SugarParameter("@id", id),
            new SugarParameter("@roleId", roleId),
            new SugarParameter("@createdAt", now.UtcDateTime));
        }

    }

    public async Task ReplacePositionsAsync(long id, IReadOnlyList<long> positionIds, DateTimeOffset now, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await EnsureActiveUserExistsAsync(id, cancellationToken);

        await ExecuteOptionalAsync(
            "DELETE FROM sys_user_position WHERE user_id = @id",
            cancellationToken,
            new SugarParameter("@id", id));
        foreach (var positionId in positionIds)
        {
            await ExpectOneAsync(
                "INSERT INTO sys_user_position (user_id, position_id, created_at) VALUES (@id, @positionId, @createdAt)",
            cancellationToken,
            requireDeletedAtFilter: false,
            new SugarParameter("@id", id),
            new SugarParameter("@positionId", positionId),
            new SugarParameter("@createdAt", now.UtcDateTime));
        }
    }

    public async Task<IReadOnlyList<long>> ListLockedRoleIdsByUserAsync(long userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var lockRoleIdsSql =
            """
            SELECT r.id
            FROM sys_role r
            INNER JOIN sys_user_role ur ON ur.role_id = r.id
            WHERE ur.user_id = @userId
              AND r.is_locked = TRUE
              AND r.deleted_at IS NULL
            ORDER BY r.id
            FOR UPDATE
            """;
        RawSqlFilterGuard.RequireDeletedAtFilter(lockRoleIdsSql, nameof(ListLockedRoleIdsByUserAsync));
        return await _db.Ado.SqlQueryAsync<long>(
            lockRoleIdsSql,
            new SugarParameter("@userId", userId));
    }

    public async Task<IReadOnlySet<long>> ExistingLockedRoleIdsAsync(IReadOnlyList<long> roleIds, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (roleIds.Count == 0)
        {
            return new HashSet<long>();
        }

        var parameters = roleIds.Select((id, index) => new SugarParameter($"@id{index}", id)).ToArray();
        var placeholders = string.Join(", ", parameters.Select(parameter => parameter.ParameterName));
        var rowsSql =
            $"""
            SELECT id
            FROM sys_role
            WHERE id IN ({placeholders})
              AND is_locked = TRUE
              AND deleted_at IS NULL
            """;
        RawSqlFilterGuard.RequireDeletedAtFilter(rowsSql, nameof(ExistingLockedRoleIdsAsync));
        var rows = await _db.Ado.SqlQueryAsync<long>(
            rowsSql,
            parameters);

        return rows.ToHashSet();
    }

    public async Task<int> CountEnabledUsersByRoleForUpdateAsync(long roleId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var rowsSql =
            """
            SELECT u.id
            FROM sys_user u
            INNER JOIN sys_user_role ur ON ur.user_id = u.id
            WHERE ur.role_id = @roleId
              AND u.status = 'enabled'
              AND u.deleted_at IS NULL
            ORDER BY u.id
            FOR UPDATE
            """;
        RawSqlFilterGuard.RequireDeletedAtFilter(rowsSql, nameof(CountEnabledUsersByRoleForUpdateAsync));
        var rows = await _db.Ado.SqlQueryAsync<long>(
            rowsSql,
            new SugarParameter("@roleId", roleId));

        return rows.Count;
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

    public Task RecordSecurityEventAsync(UserSecurityEventRecord record, CancellationToken cancellationToken)
    {
        var classification = _securityEventClassifier.Classify(record.EventType, record.TraceId);
        return ExpectOneAsync(
            """
            INSERT INTO sys_security_event (event_type, user_id, username, ip, severity, source, message, trace_id, created_at)
            VALUES (@eventType, @userId, @username, @ip, @severity, @source, @message, @traceId, @createdAt)
            """,
            cancellationToken,
            new SugarParameter("@eventType", classification.EventType),
            new SugarParameter("@userId", record.UserId),
            new SugarParameter("@username", record.Username),
            new SugarParameter("@ip", record.Ip),
            new SugarParameter("@severity", classification.Severity),
            new SugarParameter("@source", classification.Source),
            new SugarParameter("@message", record.Message),
            new SugarParameter("@traceId", classification.TraceId),
            new SugarParameter("@createdAt", record.CreatedAt.UtcDateTime));
    }

}
