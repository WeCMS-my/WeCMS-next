namespace WeCms.Modules.Organization.Departments;

public interface IDepartmentRepository
{
    Task<IReadOnlyList<DepartmentSummaryDto>> ListAsync(CancellationToken cancellationToken);
    Task<DepartmentDetailDto?> GetAsync(long id, CancellationToken cancellationToken);
    Task<bool> CodeExistsAsync(string code, long? exceptDepartmentId, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(long id, CancellationToken cancellationToken);
    Task<bool> HasChildrenAsync(long id, CancellationToken cancellationToken);
    Task<bool> HasUsersAsync(long id, CancellationToken cancellationToken);
    Task<bool> IsDescendantAsync(long id, long candidateParentId, CancellationToken cancellationToken);
    Task<long> CreateAsync(DepartmentCreateRecord record, CancellationToken cancellationToken);
    Task UpdateAsync(DepartmentUpdateRecord record, CancellationToken cancellationToken);
    Task SoftDeleteAsync(long id, DateTimeOffset now, CancellationToken cancellationToken);
    Task SetStatusAsync(long id, string status, DateTimeOffset now, CancellationToken cancellationToken);
    Task RecordAuditAsync(DepartmentAuditRecord record, CancellationToken cancellationToken);
}
