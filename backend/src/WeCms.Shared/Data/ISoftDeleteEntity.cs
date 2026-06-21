namespace WeCms.Shared.Data;

public interface ISoftDeleteEntity
{
    DateTime? DeletedAt { get; set; }
}
