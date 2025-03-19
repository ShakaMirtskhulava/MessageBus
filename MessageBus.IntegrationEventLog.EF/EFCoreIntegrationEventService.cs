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

    public EFCoreIntegrationEventService(TContext dbContext, IUnitOfWork unitOfWork, 
        IIntegrationEventLogService integrationEventLogService, IEventBus eventBus)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _integrationEventLogService = integrationEventLogService;
        _eventBus = eventBus;
    }

    public async Task<IEnumerable<IntegrationEvent>> GetPendingEvents(int batchSize, string eventTyepsAssemblyName, CancellationToken cancellationToken)
    {
        //TODO: Retrieve the types of all the existing events here 
        //SO for each event log, we can deserialize the content to the correct type
        //And don't have to use reflection to get the type for each event log
        var assembly = Assembly.Load(eventTyepsAssemblyName);
        var pendingEventLogs = await _integrationEventLogService.RetrievePendingEventLogs(batchSize, cancellationToken);
        if (pendingEventLogs.Any())
        {
            foreach (var pendingEventLog in pendingEventLogs)
            {
                var eventType = assembly.GetType(pendingEventLog.EventTypeName);
                pendingEventLog.DeserializeJsonContent(eventType!);
            }
        }
            
        return pendingEventLogs.Select(e => e.IntegrationEvent).ToList();
    }

    public async Task<IntegrationEvent> SaveAndPublish(IntegrationEvent evt,CancellationToken cancellationToken)
    {
        var @event = await _unitOfWork.ExecuteOnDefaultStarategy(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
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

        try
        {
            await _integrationEventLogService.MarkEventAsInProgress(@event.Id, cancellationToken);
            await _eventBus.PublishAsync(evt!);
            await _integrationEventLogService.MarkEventAsPublished(@event.Id, cancellationToken);
        }
        catch
        {
            await _integrationEventLogService.MarkEventAsFailed(@event.Id, cancellationToken);
            throw;
        }

        return @event;
    }

    public async Task<IntegrationEvent> Save(IntegrationEvent evt, CancellationToken cancellationToken)
    {
        return await _unitOfWork.ExecuteOnDefaultStarategy(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
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