using MessageBus.Events;
using MessageBus.IntegrationEventLog.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace MessageBus.IntegrationEventLog.EF.Models;

public class EFCoreIntegrationEventLog : IIntegrationEventLog
{
    private static readonly JsonSerializerOptions s_indentedOptions = new() { WriteIndented = true };
    private static readonly JsonSerializerOptions s_caseInsensitiveOptions = new() { PropertyNameCaseInsensitive = true };

    private EFCoreIntegrationEventLog() { }
    
    [SetsRequiredMembers]
    public EFCoreIntegrationEventLog(IntegrationEvent @event)
    {
        EventId = @event.Id;
        CreationTime = @event.CreationDate;
        var eventTypeName = @event.GetType().FullName;
        if (eventTypeName is null)
            throw new ArgumentNullException(nameof(eventTypeName));
        EventTypeName = eventTypeName;
        Content = JsonSerializer.Serialize(@event, @event.GetType(), s_indentedOptions);
        State = EventStateEnum.NotPublished;
        TimesSent = 0;
        IntegrationEvent = @event;
        EntityId = @event.EntityId!.ToString()!;
    }
    public Guid EventId { get; private set; }
    [Required]
    public required string EventTypeName { get; init; }
    [NotMapped]
    public string EventTypeShortName => EventTypeName.Split('.')!.Last();
    [NotMapped]
    public required IntegrationEvent IntegrationEvent { get; set; }
    [Required]
    public string? EntityId { get; private set; }
    public EventStateEnum State { get; set; }
    public int TimesSent { get; set; }
    public DateTime CreationTime { get; private set; }
    [Required]
    public required string Content { get; init; }

    public IIntegrationEventLog DeserializeJsonContent(Type type)
    {
        var deserializationResult = JsonSerializer.Deserialize(Content, type, s_caseInsensitiveOptions);
        if (deserializationResult is not Events.IntegrationEvent)
            throw new InvalidOperationException($"Cannot deserialize content: {Content}");
        var integrationEvent = deserializationResult as IntegrationEvent;
        IntegrationEvent = integrationEvent!;
        return this;
    }
}