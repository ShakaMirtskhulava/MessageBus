namespace MessageBus.IntegrationEventLog.Models;

public enum EventStateEnum
{
    NotPublished = 0,
    InProgress = 1,
    Published = 2,
    PublishedFailed = 3
}
