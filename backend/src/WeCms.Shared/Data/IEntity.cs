namespace WeCms.Shared.Data;

public interface IEntity<TKey>
{
    TKey Id { get; set; }
}
