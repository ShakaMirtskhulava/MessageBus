using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MessageBus.IntegrationEventLog.EF;

public class FailedMessageChainEF : IFailedMessageChain<FailedMessageEF>
{
    [Key]
    public int Id { get; set; }
    [Required]
    public DateTime CreationTime { get; set; } = DateTime.UtcNow;
    [Required]
    public int Version { get; set; }
    [Required]
    public required string EntityId { get; set; }

    [NotMapped]
    public ICollection<FailedMessageEF>? FailedMessages { get; set; }
}
