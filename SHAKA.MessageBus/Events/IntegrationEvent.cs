using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace MessageBus.Events;

public record IntegrationEvent
{
    public IntegrationEvent()
    {
        Id = Guid.NewGuid();
        CreationDate = DateTime.UtcNow;
    }

    public IntegrationEvent(object entityId)
    {
        EntityId = entityId;
        Id = Guid.NewGuid();
        CreationDate = DateTime.UtcNow;
    }

    [JsonInclude]
    public Guid Id { get; set; }
    [JsonInclude]
    public DateTime CreationDate { get; set; }
    [JsonInclude]
    public string? CorrelationId { get; set; }
    [JsonInclude]
    public object? EntityId { get; set; }
}

