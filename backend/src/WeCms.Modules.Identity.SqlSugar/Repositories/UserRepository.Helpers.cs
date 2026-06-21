using SqlSugar;

namespace WeCms.Modules.Identity.SqlSugar.Repositories;

public sealed partial class UserRepository
{
    private async Task ExecuteOptionalAsync(string sql, CancellationToken cancellationToken, params SugarParameter[] parameters)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var rows = await _db.Ado.ExecuteCommandAsync(sql, parameters);
        if (rows < 0)
        {
            throw new InvalidOperationException($"Expected non-negative affected rows, got {rows}.");
        }
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

    private async Task<IReadOnlySet<long>> ExistingActiveRoleIdsAsync(IReadOnlyList<long> ids, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (ids.Count == 0)
        {
            return new HashSet<long>();
        }

        var parameters = ids.Select((id, index) => new SugarParameter($"@id{index}", id)).ToArray();
        var placeholders = string.Join(", ", parameters.Select(parameter => parameter.ParameterName));
        var rows = await _db.Ado.SqlQueryAsync<long>(
            $"""
            SELECT id
            FROM sys_role
            WHERE id IN ({placeholders})
              AND deleted_at IS NULL
            """,
            parameters);

        return rows.ToHashSet();
    }

    private async Task EnsureActiveUserExistsAsync(long id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var total = Convert.ToInt32(
            await _db.Ado.GetScalarAsync(
                """
                SELECT COUNT(1)
                FROM sys_user
                WHERE id = @id
                  AND deleted_at IS NULL
                """,
                new SugarParameter("@id", id)),
            global::System.Globalization.CultureInfo.InvariantCulture);
        if (total != 1)
        {
            throw new InvalidOperationException("Expected one affected row, got 0.");
        }
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

        public UserDetailDto ToDto(IReadOnlyList<long> roleIds, IReadOnlyList<long> positionIds)
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
                positionIds,
                ToOffset(CreatedAt)!.Value,
                ToOffset(UpdatedAt)!.Value);
        }
    }

    private static DateTimeOffset? ToOffset(DateTime? value)
    {
        return value is null ? null : new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc));
    }
}
