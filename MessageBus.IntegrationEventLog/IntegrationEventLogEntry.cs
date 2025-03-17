using MessageBus.Events;

namespace MessageBus.IntegrationEventLog;

public interface IIntegrationEventLogEntry
{
    Guid EventId { get; }
    string EventTypeName { get; }
    string EventTypeShortName { get; }
    IntegrationEvent IntegrationEvent { get; }
    EventStateEnum State { get; }
    int TimesSent { get; }
    DateTime CreationTime { get; }
    string Content { get; }
    Guid TransactionId { get; }
    IIntegrationEventLogEntry DeserializeJsonContent(Type type);
}