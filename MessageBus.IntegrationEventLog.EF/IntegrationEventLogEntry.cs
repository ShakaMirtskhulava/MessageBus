using MessageBus.Events;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace MessageBus.IntegrationEventLog.EF;

public class IntegrationEventLogEntry : IIntegrationEventLogEntry
{
    private static readonly JsonSerializerOptions s_indentedOptions = new() { WriteIndented = true };
    private static readonly JsonSerializerOptions s_caseInsensitiveOptions = new() { PropertyNameCaseInsensitive = true };

    private IntegrationEventLogEntry() { }
    
    [SetsRequiredMembers]
    public IntegrationEventLogEntry(IntegrationEvent @event, Guid transactionId)
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
        TransactionId = transactionId;
        IntegrationEvent = @event;
    }
    public Guid EventId { get; private set; }
    [Required]
    public required string EventTypeName { get; init; }
    [NotMapped]
    public string EventTypeShortName => EventTypeName.Split('.')!.Last();
    [NotMapped]
    public required IntegrationEvent IntegrationEvent { get; set; }
    public EventStateEnum State { get; set; }
    public int TimesSent { get; set; }
    public DateTime CreationTime { get; private set; }
    [Required]
    public required string Content { get; init; }
    public Guid TransactionId { get; private set; }

    public IIntegrationEventLogEntry DeserializeJsonContent(Type type)
    {
        if(JsonSerializer.Deserialize(Content, type, s_caseInsensitiveOptions) is not IntegrationEvent integrationEvent)
            throw new InvalidOperationException($"Cannot deserialize content: {Content}");
        IntegrationEvent = integrationEvent;
        return this;
    }
}
