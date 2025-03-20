using MessageBus.Events;

namespace MessageBus.IntegrationEventLog;

public interface IIntegrationEventService
{
    Task<IEnumerable<IntegrationEvent>> GetPendingEvents(int batchSize, string eventTyepsAssemblyName, CancellationToken cancellationToken);
    Task<IntegrationEvent> SaveAndPublish<TEntity, TEntityKey>(TEntity entity, IntegrationEvent evt, CancellationToken cancellationToken)
        where TEntity : class, IEntity<TEntityKey>
        where TEntityKey : struct, IEquatable<TEntityKey>;
    Task<IntegrationEvent> Save<TEntity, TEntityKey>(TEntity entity, IntegrationEvent evt, CancellationToken cancellationToken)
        where TEntity : class, IEntity<TEntityKey>
        where TEntityKey : struct, IEquatable<TEntityKey>;
    Task Publish(IntegrationEvent evt, CancellationToken cancellationToken);
}