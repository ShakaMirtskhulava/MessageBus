using MessageBus.Events;

namespace MessageBus.IntegrationEventLog.Models;

public interface IIntegrationEventLog
{
    Guid EventId { get; }
    string EventTypeName { get; }
    string EventTypeShortName { get; }
    IntegrationEvent IntegrationEvent { get; }
    EventStateEnum State { get; }
    int TimesSent { get; }
    DateTime CreationTime { get; }
    string Content { get; }
    IIntegrationEventLog DeserializeJsonContent(Type type);
}
