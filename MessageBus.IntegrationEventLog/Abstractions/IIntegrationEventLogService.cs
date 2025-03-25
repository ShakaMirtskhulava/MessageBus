using MessageBus.Events;
using MessageBus.IntegrationEventLog.Models;

namespace MessageBus.IntegrationEventLog.Abstractions;

public interface IIntegrationEventLogService
{
    Task<IEnumerable<IIntegrationEventLog>> RetrievePendingEventLogs(int batchSize, CancellationToken cancellationToken);
    Task<TIntegrationEventLog> SaveEvent<TIntegrationEventLog>(IntegrationEvent @event, CancellationToken cancellationToken)
        where TIntegrationEventLog : class, IIntegrationEventLog;
    Task MarkEventAsPublished(Guid eventId, CancellationToken cancellationToken);
    Task MarkEventAsInProgress(Guid eventId, CancellationToken cancellationToken);
    Task MarkEventAsFailed(Guid eventId, CancellationToken cancellationToken);
    Task<bool> FailedMessageChainExists(string? entityId, CancellationToken cancellationToken);
    Task AddInFailedMessageChain(string? entityId, string eventShortName, string body, Exception? exception, CancellationToken cancellationToken);
}
