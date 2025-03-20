using MessageBus.Abstractions;
using MessageBus.Events;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace MessageBus.IntegrationEventLog.EF;

public class EFCoreIntegrationEventService<TContext> : IIntegrationEventService where TContext : DbContext
{
    private readonly DbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIntegrationEventLogService _integrationEventLogService;
    private readonly IEventBus _eventBus;
    private readonly Type[] _eventTypes;

    public EFCoreIntegrationEventService(TContext dbContext, IUnitOfWork unitOfWork, 
        IIntegrationEventLogService integrationEventLogService, IEventBus eventBus,
        string eventTyepsAssemblyName)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _integrationEventLogService = integrationEventLogService;
        _eventBus = eventBus;
        _eventTypes = Assembly.Load(eventTyepsAssemblyName).GetTypes()
            .Where(t => t.IsSubclassOf(typeof(IntegrationEvent))).ToArray();
    }

    public async Task<IEnumerable<IntegrationEvent>> GetPendingEvents(int batchSize, string eventTyepsAssemblyName, CancellationToken cancellationToken)
    {
        var pendingEventLogs = await _integrationEventLogService.RetrievePendingEventLogs(batchSize, cancellationToken);
        if (pendingEventLogs.Any())
        {
            foreach (var pendingEventLog in pendingEventLogs)
            {
                var eventType = _eventTypes.Single(t => t.Name == pendingEventLog.EventTypeShortName);
                pendingEventLog.DeserializeJsonContent(eventType);
            }
        }
            
        return pendingEventLogs.Select(e => e.IntegrationEvent).ToList();
    }

    public async Task<IntegrationEvent> SaveAndPublish<TEntity, TEntityKey>(TEntity entity, IntegrationEvent evt,CancellationToken cancellationToken) 
        where TEntity : class,IEntity<TEntityKey>
        where TEntityKey : struct, IEquatable<TEntityKey>
    {
        var @event = await Save<TEntity,TEntityKey>(entity,evt, cancellationToken);
        await Publish(@event, cancellationToken);
        return @event;
    }

    public async Task<IntegrationEvent> Save<TEntity, TEntityKey>(TEntity entity,IntegrationEvent evt, CancellationToken cancellationToken)
        where TEntity : class, IEntity<TEntityKey>
        where TEntityKey : struct, IEquatable<TEntityKey>
    {
        return await _unitOfWork.ExecuteOnDefaultStarategy(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var insertedEntity = await _dbContext.Set<TEntity>().AddAsync(entity, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
                evt.EntityId = insertedEntity.Entity.Id;
                await _integrationEventLogService.SaveEvent<EFCoreIntegrationEventLog>(evt, cancellationToken);
                await transaction.CommitAsync();
                return evt;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                await transaction.RollbackAsync();
                throw;
            }
        });
    }

    public async Task Publish(IntegrationEvent evt, CancellationToken cancellationToken)
    {
        try
        {
            await _integrationEventLogService.MarkEventAsInProgress(evt.Id, cancellationToken);
            await _eventBus.PublishAsync(evt!);
            await _integrationEventLogService.MarkEventAsPublished(evt.Id, cancellationToken);
        }
        catch
        {
            await _integrationEventLogService.MarkEventAsFailed(evt.Id, cancellationToken);
            throw;
        }
    }

}