using WeCms.Shared;

namespace WeCms.Modules.Organization.Departments;

public sealed class DepartmentService : IDepartmentService
{
    private readonly IDepartmentRepository _repository;

    public DepartmentService(IDepartmentRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<DepartmentSummaryDto>> ListAsync(CancellationToken cancellationToken) => _repository.ListAsync(cancellationToken);

    public async Task<IReadOnlyList<DepartmentTreeDto>> TreeAsync(CancellationToken cancellationToken)
    {
        var departments = await _repository.ListAsync(cancellationToken);
        return BuildTree(departments, null);
    }

    public async Task<DepartmentDetailDto> GetAsync(long id, CancellationToken cancellationToken)
    {
        return await _repository.GetAsync(id, cancellationToken)
            ?? throw new DomainException(ApiCodes.NotFound, "Department was not found.");
    }

    public async Task<DepartmentMutationResponse> CreateAsync(CreateDepartmentRequest request, DepartmentRequestContext context, CancellationToken cancellationToken)
    {
        var code = NormalizeRequired(request.Code, "code", 80);
        var name = NormalizeRequired(request.Name, "name", 120);
        var status = NormalizeStatus(request.Status);
        await EnsureCodeUniqueAsync(code, null, cancellationToken);
        await EnsureParentAsync(null, request.ParentId, cancellationToken);

        var id = await _repository.CreateAsync(new DepartmentCreateRecord(request.ParentId, code, name, request.SortOrder, status, context.Now), cancellationToken);
        await AuditAsync(context, "create", id, "success", "Department created.", cancellationToken);
        return new DepartmentMutationResponse(id);
    }

    public async Task<DepartmentMutationResponse> UpdateAsync(long id, UpdateDepartmentRequest request, DepartmentRequestContext context, CancellationToken cancellationToken)
    {
        _ = await GetAsync(id, cancellationToken);
        var name = NormalizeRequired(request.Name, "name", 120);
        var status = NormalizeStatus(request.Status);
        await EnsureParentAsync(id, request.ParentId, cancellationToken);
        await _repository.UpdateAsync(new DepartmentUpdateRecord(id, request.ParentId, name, request.SortOrder, status, context.Now), cancellationToken);
        await AuditAsync(context, "update", id, "success", "Department updated.", cancellationToken);
        return new DepartmentMutationResponse(id);
    }

    public async Task DeleteAsync(long id, DepartmentRequestContext context, CancellationToken cancellationToken)
    {
        _ = await GetAsync(id, cancellationToken);
        if (await _repository.HasChildrenAsync(id, cancellationToken))
        {
            throw new DomainException(ApiCodes.BusinessError, "Department with child departments cannot be deleted.");
        }

        if (await _repository.HasUsersAsync(id, cancellationToken))
        {
            throw new DomainException(ApiCodes.BusinessError, "Department assigned to users cannot be deleted.");
        }

        await _repository.SoftDeleteAsync(id, context.Now, cancellationToken);
        await AuditAsync(context, "delete", id, "success", "Department deleted.", cancellationToken);
    }

    public async Task EnableAsync(long id, DepartmentRequestContext context, CancellationToken cancellationToken)
    {
        _ = await GetAsync(id, cancellationToken);
        await _repository.SetStatusAsync(id, "enabled", context.Now, cancellationToken);
        await AuditAsync(context, "enable", id, "success", "Department enabled.", cancellationToken);
    }

    public async Task DisableAsync(long id, DepartmentRequestContext context, CancellationToken cancellationToken)
    {
        _ = await GetAsync(id, cancellationToken);
        await _repository.SetStatusAsync(id, "disabled", context.Now, cancellationToken);
        await AuditAsync(context, "disable", id, "success", "Department disabled.", cancellationToken);
    }

    private async Task EnsureParentAsync(long? currentDepartmentId, long? parentId, CancellationToken cancellationToken)
    {
        if (parentId is null)
        {
            return;
        }

        if (parentId <= 0)
        {
            throw Validation("parentId must be positive.");
        }

        if (currentDepartmentId == parentId)
        {
            throw new DomainException(ApiCodes.BusinessError, "Department cannot be its own parent.");
        }

        if (!await _repository.ExistsAsync(parentId.Value, cancellationToken))
        {
            throw Validation("parentId does not exist.");
        }

        if (currentDepartmentId is not null && await _repository.IsDescendantAsync(currentDepartmentId.Value, parentId.Value, cancellationToken))
        {
            throw new DomainException(ApiCodes.BusinessError, "Department parent cannot be a descendant.");
        }
    }

    private async Task EnsureCodeUniqueAsync(string code, long? exceptDepartmentId, CancellationToken cancellationToken)
    {
        if (await _repository.CodeExistsAsync(code, exceptDepartmentId, cancellationToken))
        {
            throw new DomainException(ApiCodes.Conflict, "department code already exists.");
        }
    }

    private Task AuditAsync(DepartmentRequestContext context, string action, long targetDepartmentId, string result, string detail, CancellationToken cancellationToken)
    {
        return _repository.RecordAuditAsync(new DepartmentAuditRecord(context.ActorUserId, context.ActorUsername, action, targetDepartmentId, context.Ip, context.UserAgent, context.TraceId, result, detail, context.Now), cancellationToken);
    }

    private static IReadOnlyList<DepartmentTreeDto> BuildTree(IReadOnlyList<DepartmentSummaryDto> departments, long? parentId)
    {
        return departments
            .Where(department => department.ParentId == parentId)
            .OrderBy(department => department.SortOrder)
            .ThenBy(department => department.Id)
            .Select(department => new DepartmentTreeDto(department.Id, department.ParentId, department.Code, department.Name, department.SortOrder, department.Status, BuildTree(departments, department.Id)))
            .ToArray();
    }

    private static string NormalizeStatus(string value)
    {
        var normalized = NormalizeRequired(value, "status", 32);
        return normalized is "enabled" or "disabled" ? normalized : throw Validation("status must be enabled or disabled.");
    }

    private static string NormalizeRequired(string? value, string name, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw Validation($"{name} is required.");
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw Validation($"value must be {maxLength} characters or fewer.");
        }

        return normalized;
    }

    private static DomainException Validation(string message) => new(ApiCodes.ValidationError, message);
}
