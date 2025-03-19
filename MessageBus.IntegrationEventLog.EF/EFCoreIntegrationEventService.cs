using MessageBus.Abstractions;
using MessageBus.Events;
using Microsoft.EntityFrameworkCore;

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
}