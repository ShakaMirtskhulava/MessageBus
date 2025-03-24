namespace MessageBus.IntegrationEventLog;

public interface IFailedMessageChain<TFailedMessage> where TFailedMessage : IFailedMessage
{
    int Id { get; set; }
    DateTime CreationTime { get; set; }
    int Version { get; set; }
    string EntityId { get; set; }

    ICollection<TFailedMessage>? FailedMessages { get; set; }
}
