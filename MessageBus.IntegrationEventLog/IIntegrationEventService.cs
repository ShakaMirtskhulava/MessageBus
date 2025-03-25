using MessageBus.Events;

namespace MessageBus.IntegrationEventLog;

public interface IIntegrationEventService
{
    Task<IEnumerable<IntegrationEvent>> GetPendingEvents(int batchSize, string eventTyepsAssemblyName, CancellationToken cancellationToken);
    Task<IEnumerable<IntegrationEvent>> RetriveFailedEventsToRepublish(int chainBatchSize, CancellationToken cancellationToken);
    Task<IntegrationEvent> Add<TEntity, TEntityKey>(TEntity entity, IntegrationEvent evt, CancellationToken cancellationToken)
        where TEntity : class, IEntity<TEntityKey>
        where TEntityKey : struct, IEquatable<TEntityKey>;
    Task<IntegrationEvent> Update<TEntity, TEntityKey>(TEntity entity, IntegrationEvent evt, CancellationToken cancellationToken)
        where TEntity : class, IEntity<TEntityKey>
        where TEntityKey : struct, IEquatable<TEntityKey>;
    Task<IntegrationEvent> Remove<TEntity, TEntityKey>(TEntity entity, IntegrationEvent evt, CancellationToken cancellationToken)
        where TEntity : class, IEntity<TEntityKey>
        where TEntityKey : struct, IEquatable<TEntityKey>;
}