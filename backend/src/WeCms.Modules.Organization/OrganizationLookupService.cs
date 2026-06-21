using WeCms.Modules.Organization.Departments;
using WeCms.Modules.Organization.Positions;

namespace WeCms.Modules.Organization;

public interface IOrganizationLookupService
{
    Task<bool> DepartmentExistsAsync(long departmentId, CancellationToken cancellationToken);

    Task<IReadOnlySet<long>> ExistingPositionIdsAsync(IReadOnlyList<long> positionIds, CancellationToken cancellationToken);
}

public sealed class OrganizationLookupService : IOrganizationLookupService
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IPositionRepository _positionRepository;

    public OrganizationLookupService(
        IDepartmentRepository departmentRepository,
        IPositionRepository positionRepository)
    {
        _departmentRepository = departmentRepository;
        _positionRepository = positionRepository;
    }

    public Task<bool> DepartmentExistsAsync(long departmentId, CancellationToken cancellationToken)
    {
        return _departmentRepository.ExistsAsync(departmentId, cancellationToken);
    }

    public Task<IReadOnlySet<long>> ExistingPositionIdsAsync(IReadOnlyList<long> positionIds, CancellationToken cancellationToken)
    {
        return _positionRepository.ExistingIdsAsync(positionIds, cancellationToken);
    }
}
