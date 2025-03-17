using MessageBus.Events;

namespace MessageBus.IntegrationEventLog;

public interface IIntegrationEventLogService
{
    Task<IEnumerable<IIntegrationEventLogEntry>> RetrievePendingEventLogs(Guid transactionId,CancellationToken cancellationToken);
    Task SaveEventAsync(IIntegrationEventLogEntry @event, CancellationToken cancellationToken);
    Task MarkEventAsPublishedAsync(Guid eventId, CancellationToken cancellationToken);
    Task MarkEventAsInProgressAsync(Guid eventId, CancellationToken cancellationToken);
    Task MarkEventAsFailedAsync(Guid eventId, CancellationToken cancellationToken);
}
