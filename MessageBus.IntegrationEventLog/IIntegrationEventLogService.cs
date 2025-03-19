using MessageBus.Events;

namespace MessageBus.IntegrationEventLog;

public interface IIntegrationEventLogService
{
    Task<IEnumerable<IIntegrationEventLog>> RetrievePendingEventLogs(int batchSize, CancellationToken cancellationToken);
    Task<TIntegrationEventLog> SaveEvent<TIntegrationEventLog>(IntegrationEvent @event, CancellationToken cancellationToken)
        where TIntegrationEventLog : class, IIntegrationEventLog;
    Task MarkEventAsPublished(Guid eventId, CancellationToken cancellationToken);
    Task MarkEventAsInProgress(Guid eventId, CancellationToken cancellationToken);
    Task MarkEventAsFailed(Guid eventId, CancellationToken cancellationToken);
}
