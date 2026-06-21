namespace WeCms.Shared.Data;

public interface IAuditedEntity
{
    DateTime CreatedAt { get; set; }

    DateTime UpdatedAt { get; set; }
}
