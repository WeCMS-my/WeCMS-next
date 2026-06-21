namespace WeCms.EventBus;

public interface IOutboxDispatcher
{
    Task DispatchAsync(CancellationToken cancellationToken);
}
