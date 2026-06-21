using System.Text.Json;

namespace WeCms.EventBus;

public sealed class SystemTextJsonIntegrationEventSerializer : IIntegrationEventSerializer
{
    private readonly IReadOnlyDictionary<string, Type> eventTypes;

    public SystemTextJsonIntegrationEventSerializer(IEnumerable<IntegrationEventRegistration> registrations)
    {
        ArgumentNullException.ThrowIfNull(registrations);

        eventTypes = registrations.ToDictionary(
            static registration => registration.EventType,
            static registration => registration.EventClrType,
            StringComparer.Ordinal);
    }

    public IIntegrationEvent Deserialize(string eventType, string payloadJson)
    {
        if (!eventTypes.TryGetValue(eventType, out var eventClrType))
        {
            throw new InvalidOperationException($"Integration event type '{eventType}' is not registered.");
        }

        if (!typeof(IIntegrationEvent).IsAssignableFrom(eventClrType))
        {
            throw new InvalidOperationException($"Registered event type '{eventClrType.FullName}' does not implement IIntegrationEvent.");
        }

        var integrationEvent = JsonSerializer.Deserialize(payloadJson, eventClrType) as IIntegrationEvent;
        return integrationEvent ?? throw new InvalidOperationException($"Integration event payload for '{eventType}' could not be deserialized.");
    }
}
