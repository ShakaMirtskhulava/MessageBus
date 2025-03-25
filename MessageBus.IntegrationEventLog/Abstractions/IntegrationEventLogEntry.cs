using MessageBus.Events;
using MessageBus.IntegrationEventLog.Models;

namespace MessageBus.IntegrationEventLog.Abstractions;

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
