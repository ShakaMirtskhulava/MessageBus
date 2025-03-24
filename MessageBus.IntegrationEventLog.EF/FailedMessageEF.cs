using System.ComponentModel.DataAnnotations;

namespace MessageBus.IntegrationEventLog.EF;

public class FailedMessageEF : IFailedMessage
{
    [Key]
    public int Id { get; set; }
    [Required]
    public DateTime CreationTime { get; set; } = DateTime.UtcNow;
    [Required]
    public required string Body { get; set; }
    public string? Message { get; set; }
    public string? StackTrace { get; set; }

    public int FailedMessageChainId { get; set; }
    public FailedMessageChainEF? FailedMessageChain { get; set; }
}
