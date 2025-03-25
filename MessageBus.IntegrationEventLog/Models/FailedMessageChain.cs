namespace MessageBus.IntegrationEventLog.Models;

public interface IFailedMessageChain<TFailedMessage> where TFailedMessage : IFailedMessage
{
    int Id { get; set; }
    DateTime CreationTime { get; set; }
    string EntityId { get; set; }
    public bool ShouldRepublish { get; set; }

    ICollection<TFailedMessage>? FailedMessages { get; set; }
}
