using MessageBus.Events;

namespace MessageBus.IntegrationEventLog;

public interface IFailedMessage
{
    int Id { get; set; }
    DateTime CreationTime { get; set; }
    string Body { get; set; }
    string? Message { get; set; }
    string? StackTrace { get; set; }
    string EventTypeShortName { get; set; }
    IntegrationEvent? IntegrationEvent { get; set; }
    bool ShouldSkip { get; set; }
    IFailedMessage DeserializeJsonBody(Type type);
}