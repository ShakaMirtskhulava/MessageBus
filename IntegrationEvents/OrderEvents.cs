using MessageBus.Events;
using System.Text.Json.Serialization;

namespace MessageBus.Example.IntegrationEvents;


public record OrderCreated(Guid OrderId, string data) : IntegrationEvent(OrderId);
public record OrderUpdated(Guid OrderId, string data) : IntegrationEvent(OrderId);
public record OrderDeleted(Guid OrderId) : IntegrationEvent(OrderId);

public record ToastCreated(int ToastId, string data) : IntegrationEvent(ToastId);