using MessageBus.Events;

namespace MessageBus.Example.IntegrationEvents;


public record OrderCreated(Guid OrderId, string data) : IntegrationEvent;
public record OrderUpdated(Guid OrderId, string data) : IntegrationEvent;
public record OrderDeleted(Guid OrderId) : IntegrationEvent;