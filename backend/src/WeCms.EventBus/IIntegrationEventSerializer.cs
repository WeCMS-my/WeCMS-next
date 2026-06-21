namespace WeCms.EventBus;

public interface IIntegrationEventSerializer
{
    IIntegrationEvent Deserialize(string eventType, string payloadJson);
}
