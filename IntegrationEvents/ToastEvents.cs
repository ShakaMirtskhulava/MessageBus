using MessageBus.Events;

namespace MessageBus.Example.IntegrationEvents;

public record ToastCreated(int ToastId, string data) : IntegrationEvent(ToastId);
