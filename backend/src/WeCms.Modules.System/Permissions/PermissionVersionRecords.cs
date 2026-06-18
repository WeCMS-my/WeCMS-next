namespace WeCms.Modules.System.Permissions;

public interface IPermissionVersionService
{
    Task BumpUserAsync(long userId, DateTimeOffset now, CancellationToken cancellationToken);
    Task BumpUsersByRoleAsync(long roleId, DateTimeOffset now, CancellationToken cancellationToken);
    Task BumpUsersByPermissionAsync(long permissionId, DateTimeOffset now, CancellationToken cancellationToken);
    Task BumpUsersByMenuAsync(long menuId, DateTimeOffset now, CancellationToken cancellationToken);
    Task BumpUsersByMenusAsync(IReadOnlyList<long> menuIds, DateTimeOffset now, CancellationToken cancellationToken);
}

public interface IPermissionVersionRepository
{
    Task BumpUserAsync(long userId, DateTimeOffset now, CancellationToken cancellationToken);
    Task BumpUsersByRoleAsync(long roleId, DateTimeOffset now, CancellationToken cancellationToken);
    Task BumpUsersByPermissionAsync(long permissionId, DateTimeOffset now, CancellationToken cancellationToken);
    Task BumpUsersByMenuAsync(long menuId, DateTimeOffset now, CancellationToken cancellationToken);
}

public sealed class PermissionVersionService : IPermissionVersionService
{
    private const int MaxBatchSize = 200;
    private readonly IPermissionVersionRepository _repository;

    public PermissionVersionService(IPermissionVersionRepository repository)
    {
        _repository = repository;
    }

    public Task BumpUserAsync(long userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        EnsurePositive(userId, nameof(userId));
        return _repository.BumpUserAsync(userId, now, cancellationToken);
    }

    public Task BumpUsersByRoleAsync(long roleId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        EnsurePositive(roleId, nameof(roleId));
        return _repository.BumpUsersByRoleAsync(roleId, now, cancellationToken);
    }

    public Task BumpUsersByPermissionAsync(long permissionId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        EnsurePositive(permissionId, nameof(permissionId));
        return _repository.BumpUsersByPermissionAsync(permissionId, now, cancellationToken);
    }

    public Task BumpUsersByMenuAsync(long menuId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        EnsurePositive(menuId, nameof(menuId));
        return _repository.BumpUsersByMenuAsync(menuId, now, cancellationToken);
    }

    public async Task BumpUsersByMenusAsync(IReadOnlyList<long> menuIds, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (menuIds.Count > MaxBatchSize)
        {
            throw new InvalidOperationException($"menuIds must contain at most {MaxBatchSize} items.");
        }

        foreach (var menuId in menuIds.Distinct())
        {
            await BumpUsersByMenuAsync(menuId, now, cancellationToken);
        }
    }

    private static void EnsurePositive(long value, string name)
    {
        if (value <= 0)
        {
            throw new InvalidOperationException($"{name} must be positive.");
        }
    }
}
