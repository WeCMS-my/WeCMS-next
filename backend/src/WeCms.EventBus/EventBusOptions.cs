namespace WeCms.EventBus;

public sealed class EventBusOptions
{
    public EventBusHandlerFailureBehavior HandlerFailureBehavior { get; set; } = EventBusHandlerFailureBehavior.Rethrow;
}
