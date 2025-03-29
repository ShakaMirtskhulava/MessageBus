using MessageBus.Events;
using System.Text.Json.Serialization;

namespace MessageBus.Example.IntegrationEvents;


public record OrderCreated : IntegrationEvent
{
    public string Data { get; init; }

    public OrderCreated(Guid orderId, string data) : base(orderId)
    {
        Data = data;
    }

    [JsonConstructor]
    public OrderCreated(Guid id, string data, DateTime creationDate, string? correlationId, object entityId) : base(entityId)
    {
        Id = id;
        Data = data;
        CreationDate = creationDate;
        CorrelationId = correlationId;
        EntityId = entityId;
    }
}


public record OrderUpdated : IntegrationEvent
{
    public string Data { get; init; }

    public OrderUpdated(Guid orderId, string data) : base(orderId)
    {
        Data = data;
    }

    [JsonConstructor]
    public OrderUpdated(Guid id, string data, DateTime creationDate, string? correlationId, object entityId) : base(entityId)
    {
        Id = id;
        Data = data;
        CreationDate = creationDate;
        CorrelationId = correlationId;
        EntityId = entityId;
    }
}

public record OrderDeleted : IntegrationEvent
{
    public string Data { get; init; }

    public OrderDeleted(Guid orderId, string data) : base(orderId)
    {
        Data = data;
    }

    [JsonConstructor]
    public OrderDeleted(Guid id, string data, DateTime creationDate, string? correlationId, object entityId) : base(entityId)
    {
        Id = id;
        Data = data;
        CreationDate = creationDate;
        CorrelationId = correlationId;
        EntityId = entityId;
    }
}
