using MessageBus.Events;

namespace MessageBus.Example.IntegrationEvents;


public record OrderCreated(int OrderId, string data) : IntegrationEvent;
public record OrderUpdated(int OrderId, string data) : IntegrationEvent;
public record OrderDeleted(int OrderId) : IntegrationEvent;