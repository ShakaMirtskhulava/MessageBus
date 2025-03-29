using MessageBus.Events;
using System.Text.Json.Serialization;

namespace MessageBus.Example.IntegrationEvents;

public record ToastCreated : IntegrationEvent
{
    public string Data { get; init; }

    public ToastCreated(int toastId, string data) : base(toastId)
    {
        Data = data;
    }

    [JsonConstructor]
    public ToastCreated(Guid id, string data, DateTime creationDate, string? correlationId, object entityId) : base(entityId)
    {
        Id = id;
        Data = data;
        CreationDate = creationDate;
        CorrelationId = correlationId;
        EntityId = entityId;
    }
}
