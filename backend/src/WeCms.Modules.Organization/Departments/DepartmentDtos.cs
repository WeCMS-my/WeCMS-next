namespace WeCms.Modules.Organization.Departments;

public sealed record DepartmentSummaryDto(long Id, long? ParentId, string Code, string Name, int SortOrder, string Status);

public sealed record DepartmentTreeDto(long Id, long? ParentId, string Code, string Name, int SortOrder, string Status, IReadOnlyList<DepartmentTreeDto> Children);

public sealed record DepartmentDetailDto(long Id, long? ParentId, string Code, string Name, int SortOrder, string Status, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

public sealed record CreateDepartmentRequest(long? ParentId, string Code, string Name, int SortOrder, string Status);

public sealed record UpdateDepartmentRequest(long? ParentId, string Name, int SortOrder, string Status);

public sealed record DepartmentMutationResponse(long Id);

public interface IDepartmentService
{
    Task<IReadOnlyList<DepartmentSummaryDto>> ListAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<DepartmentTreeDto>> TreeAsync(CancellationToken cancellationToken);
    Task<DepartmentDetailDto> GetAsync(long id, CancellationToken cancellationToken);
    Task<DepartmentMutationResponse> CreateAsync(CreateDepartmentRequest request, DepartmentRequestContext context, CancellationToken cancellationToken);
    Task<DepartmentMutationResponse> UpdateAsync(long id, UpdateDepartmentRequest request, DepartmentRequestContext context, CancellationToken cancellationToken);
    Task DeleteAsync(long id, DepartmentRequestContext context, CancellationToken cancellationToken);
    Task EnableAsync(long id, DepartmentRequestContext context, CancellationToken cancellationToken);
    Task DisableAsync(long id, DepartmentRequestContext context, CancellationToken cancellationToken);
}
