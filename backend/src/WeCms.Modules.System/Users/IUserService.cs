namespace WeCms.Modules.System.Users;

public interface IUserService
{
    Task<(IReadOnlyList<UserListItem> Items, long Total)> ListAsync(UserQueryParams q, CancellationToken ct);
    Task<UserDetail?> GetByIdAsync(long id, CancellationToken ct);
    Task<long> CreateAsync(CreateUserRequest req, long operatorId, CancellationToken ct);
    Task UpdateAsync(long id, UpdateUserRequest req, long operatorId, CancellationToken ct);
    Task DeleteAsync(long id, long operatorId, CancellationToken ct);
    Task SetStatusAsync(long id, string status, long operatorId, CancellationToken ct);
}
