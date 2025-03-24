namespace MessageBus.IntegrationEventLog;

public interface IFailedMessage
{
    int Id { get; set; }
    DateTime CreationTime { get; set; }
    string Body { get; set; }
    string? Message { get; set; }
    string? StackTrace { get; set; }
}