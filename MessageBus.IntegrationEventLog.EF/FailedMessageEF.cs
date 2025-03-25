using MessageBus.Events;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace MessageBus.IntegrationEventLog.EF;

public class FailedMessageEF : IFailedMessage
{
    private static readonly JsonSerializerOptions s_caseInsensitiveOptions = new() { PropertyNameCaseInsensitive = true };

    [Key]
    public int Id { get; set; }
    [Required]
    public DateTime CreationTime { get; set; } = DateTime.UtcNow;
    [Required]
    public required string Body { get; set; }
    [Required]
    public required string EventTypeShortName { get; set; }
    [Required]
    public bool ShouldSkip { get; set; } = false;
    [NotMapped]
    public IntegrationEvent? IntegrationEvent { get; set; }
    public string? Message { get; set; }
    public string? StackTrace { get; set; }

    public int FailedMessageChainId { get; set; }
    public FailedMessageChainEF? FailedMessageChain { get; set; }

    public IFailedMessage DeserializeJsonBody(Type type)
    {
        var deserializationResult = JsonSerializer.Deserialize(Body, type, s_caseInsensitiveOptions);
        if (deserializationResult is not Events.IntegrationEvent)
            throw new InvalidOperationException($"Cannot deserialize Body: {Body}");
        var integrationEvent = deserializationResult as IntegrationEvent;
        IntegrationEvent = integrationEvent!;
        return this;
    }
}
