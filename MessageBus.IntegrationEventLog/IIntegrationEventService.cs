using MessageBus.Events;

namespace MessageBus.IntegrationEventLog;

public interface IIntegrationEventService
{
    Task<IEnumerable<IntegrationEvent>> GetPendingEvents(int batchSize, string eventTyepsAssemblyName, CancellationToken cancellationToken);
    Task<IntegrationEvent> SaveAndPublish(IntegrationEvent evt, CancellationToken cancellationToken);
    Task<IntegrationEvent> Save(IntegrationEvent evt, CancellationToken cancellationToken);
    Task Publish(IntegrationEvent evt, CancellationToken cancellationToken);
}