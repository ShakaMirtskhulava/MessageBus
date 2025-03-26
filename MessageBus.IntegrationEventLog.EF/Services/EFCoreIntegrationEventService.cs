using MessageBus.Abstractions;
using MessageBus.Events;
using MessageBus.IntegrationEventLog.EF.Models;
using MessageBus.IntegrationEventLog.Models;
using MessageBus.IntegrationEventLog.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace MessageBus.IntegrationEventLog.EF.Services;

public class EFCoreIntegrationEventService<TContext> : IIntegrationEventService where TContext : DbContext
{
    private readonly DbContext _dbContext;
    private readonly UnitOfWork<TContext> _unitOfWork;
    private readonly IIntegrationEventLogService _integrationEventLogService;
    private readonly IEventBus _eventBus;
    private readonly Type[] _eventTypes;
    private readonly ILogger<EFCoreIntegrationEventService<TContext>> _logger;

    public EFCoreIntegrationEventService(TContext dbContext, UnitOfWork<TContext> unitOfWork,
        IIntegrationEventLogService integrationEventLogService, IEventBus eventBus,
        string eventTyepsAssemblyName, ILogger<EFCoreIntegrationEventService<TContext>> logger)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _integrationEventLogService = integrationEventLogService;
        _eventBus = eventBus;
        _eventTypes = Assembly.Load(eventTyepsAssemblyName).GetTypes()
            .Where(t => t.IsSubclassOf(typeof(IntegrationEvent))).ToArray();
        _logger = logger;
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

    public async Task<IEnumerable<IntegrationEvent>> RetriveFailedEventsToRepublish(int chainBatchSize, CancellationToken cancellationToken)
    {
        var chians = await _dbContext.Set<FailedMessageChainEF>()
                                   .Include(fmch => fmch.FailedMessages)
                                   .Where(e => e.ShouldRepublish)
                                   .OrderBy(e => e.CreationTime)
                                   .Take(chainBatchSize)
                                   .ToListAsync(cancellationToken);

        List<IntegrationEvent> failedEvents = new();
        foreach (var chain in chians)
        {
            if(chain.FailedMessages is null || !chain.FailedMessages.Any())
                continue;

            foreach (var failedMessage in chain.FailedMessages)
            {
                if (failedMessage.ShouldSkip)
                {
                    _logger.LogInformation($"Skipping failed message with body: \n{failedMessage.Body}");
                    continue;
                }
                var eventType = _eventTypes.Single(t => t.Name == failedMessage.EventTypeShortName);
                failedMessage.DeserializeJsonBody(eventType);
                failedEvents.Add(failedMessage.IntegrationEvent!);
            }
        }

        _dbContext.Set<FailedMessageChainEF>().RemoveRange(chians);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return failedEvents;
    }

    public async Task<IntegrationEvent> Add<TEntity, TEntityKey>(TEntity entity,IntegrationEvent evt, CancellationToken cancellationToken)
        where TEntity : class, IEntity<TEntityKey>
        where TEntityKey : struct, IEquatable<TEntityKey>
    {
        return await _unitOfWork.ExecuteOnDefaultStarategy(async () =>
        {
            await _unitOfWork.BeginTransaction(cancellationToken);
            try
            {
                var insertedEntity = await _dbContext.Set<TEntity>().AddAsync(entity, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
                evt.EntityId = insertedEntity.Entity.Id;
                await _integrationEventLogService.SaveEvent<EFCoreIntegrationEventLog>(evt, cancellationToken);
                await _unitOfWork.CommitTransaction(cancellationToken);
                return evt;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                await _unitOfWork.RollbackTransaction(cancellationToken);
                throw;
            }
        });
    }

    public async Task<IntegrationEvent> Update<TEntity, TEntityKey>(TEntity entity, IntegrationEvent evt, CancellationToken cancellationToken)
        where TEntity : class, IEntity<TEntityKey>
        where TEntityKey : struct, IEquatable<TEntityKey>
    {
        return await _unitOfWork.ExecuteOnDefaultStarategy(async () =>
        {
            await _unitOfWork.BeginTransaction(cancellationToken);
            try
            {
                var insertedEntity = _dbContext.Set<TEntity>().Update(entity);
                await _dbContext.SaveChangesAsync(cancellationToken);
                evt.EntityId = insertedEntity.Entity.Id;
                await _integrationEventLogService.SaveEvent<EFCoreIntegrationEventLog>(evt, cancellationToken);
                await _unitOfWork.CommitTransaction(cancellationToken);
                return evt;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                await _unitOfWork.RollbackTransaction(cancellationToken);
                throw;
            }
        });
    }

    public async Task<IntegrationEvent> Remove<TEntity, TEntityKey>(TEntity entity, IntegrationEvent evt, CancellationToken cancellationToken)
        where TEntity : class, IEntity<TEntityKey>
        where TEntityKey : struct, IEquatable<TEntityKey>
    {
        return await _unitOfWork.ExecuteOnDefaultStarategy(async () =>
        {
            await _unitOfWork.BeginTransaction(cancellationToken);
            try
            {
                var insertedEntity = _dbContext.Set<TEntity>().Remove(entity);
                await _dbContext.SaveChangesAsync(cancellationToken);
                evt.EntityId = insertedEntity.Entity.Id;
                await _integrationEventLogService.SaveEvent<EFCoreIntegrationEventLog>(evt, cancellationToken);
                await _unitOfWork.CommitTransaction(cancellationToken);
                return evt;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                await _unitOfWork.RollbackTransaction(cancellationToken);
                throw;
            }
        });
    }
}