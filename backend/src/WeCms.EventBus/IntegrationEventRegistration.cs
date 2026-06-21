namespace WeCms.EventBus;

public sealed record IntegrationEventRegistration(string EventType, Type EventClrType);
