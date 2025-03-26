using MessageBus.Events;
using MessageBus.IntegrationEventLog.EF.Models;
using MessageBus.IntegrationEventLog.Models;
using MessageBus.IntegrationEventLog.Services;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace MessageBus.IntegrationEventLog.EF.Services;

public class EFIntegrationEventLogService<TContext> : IIntegrationEventLogService, IDisposable
    where TContext : DbContext
{
    private volatile bool _disposedValue;
    private readonly TContext _context;

    public EFIntegrationEventLogService(TContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<IIntegrationEventLog>> RetrievePendingEventLogs(int batchSize,CancellationToken cancellationToken)
    {
        var result = await _context.Set<EFCoreIntegrationEventLog>()
                                   .Where(e => e.State == EventStateEnum.NotPublished)
                                   .OrderBy(e => e.CreationTime)
                                   .Take(batchSize)
                                   .ToListAsync(cancellationToken);

        return result;
    }

    public async Task<TIntegrationEventLog> SaveEvent<TIntegrationEventLog>(IntegrationEvent @event, CancellationToken cancellationToken) 
        where TIntegrationEventLog : class, IIntegrationEventLog
    {
        var integrationEventLog = new EFCoreIntegrationEventLog(@event) as TIntegrationEventLog;
        if (integrationEventLog is null)
            throw new InvalidOperationException($"Cannot cast {nameof(integrationEventLog)} to {nameof(TIntegrationEventLog)}");

        _context.Set<TIntegrationEventLog>().Add(integrationEventLog);
        await _context.SaveChangesAsync(cancellationToken);
        return integrationEventLog;
    }

    public async Task MarkEventAsPublished(Guid eventId, CancellationToken cancellationToken)
    {
        await UpdateEventStatus(eventId, EventStateEnum.Published,cancellationToken);
    }

    public async Task MarkEventAsInProgress(Guid eventId, CancellationToken cancellationToken)
    {
        await UpdateEventStatus(eventId, EventStateEnum.InProgress,cancellationToken);
    }

    public async Task MarkEventAsFailed(Guid eventId, CancellationToken cancellationToken)
    {
        await UpdateEventStatus(eventId, EventStateEnum.PublishedFailed,cancellationToken);
    }

    public async Task<bool> FailedMessageChainExists(string? entityId, CancellationToken cancellationToken)
    {
        entityId = entityId ?? "NoEntity";
        return await _context.Set<FailedMessageChainEF>()
                             .AnyAsync(fmc => fmc.EntityId == entityId)
                             .ConfigureAwait(false);
    }

    public async Task AddInFailedMessageChain(string? entityId,string eventShortName, string body, Exception? exception, CancellationToken cancellationToken)
    {
        FailedMessageChainEF? targetFiledMessageChain = null;
        targetFiledMessageChain = await _context.Set<FailedMessageChainEF>()
                                                .Include(fmc => fmc.FailedMessages)
                                                .FirstOrDefaultAsync(fmc => fmc.EntityId == entityId, cancellationToken)
                                                .ConfigureAwait(false);
        if (targetFiledMessageChain is null)
        {
            targetFiledMessageChain = new()
            {
                EntityId = entityId ?? "NoEntity",
                FailedMessages = new List<FailedMessageEF>()
                {
                    new()
                    {
                        Body = body,
                        Message = exception is not null ? GetFullExceptionMessage(exception) : null,
                        StackTrace = exception is not null ? exception.StackTrace : null,
                        EventTypeShortName = eventShortName
                    }
                }
            };
            _context.Set<FailedMessageChainEF>().Add(targetFiledMessageChain);
        }
        else
        {
            targetFiledMessageChain.FailedMessages!.Add(new()
            {
                Body = body,
                Message = exception is not null ? GetFullExceptionMessage(exception) : null,
                StackTrace = exception is not null ? exception.StackTrace : null,
                EventTypeShortName = eventShortName
            });

            _context.Set<FailedMessageChainEF>().Update(targetFiledMessageChain);
        }
        await _context.SaveChangesAsync(cancellationToken);
    }

    private string GetFullExceptionMessage(Exception exception)
    {
        StringBuilder fullExceptionMessageBuilder = new(exception.Message);
        Exception? innerException = exception.InnerException;
        while (innerException is not null)
        {
            fullExceptionMessageBuilder.AppendLine(innerException.Message);
            innerException = innerException.InnerException;
        }
        return fullExceptionMessageBuilder.ToString();
    }

    private async Task UpdateEventStatus(Guid eventId, EventStateEnum status, CancellationToken cancellationToken)
    {
        var eventLogEntry = _context.Set<EFCoreIntegrationEventLog>().Single(ie => ie.EventId == eventId);
        eventLogEntry.State = status;

        if (status == EventStateEnum.InProgress)
            eventLogEntry.TimesSent++;

        await _context.SaveChangesAsync(cancellationToken);
    }
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
                _context.Dispose();
            _disposedValue = true;
        }
    }
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

}
