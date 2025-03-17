using MessageBus.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Reflection;

namespace MessageBus.IntegrationEventLog.EF.Services;

public class EFIntegrationEventLogService<TContext> : IIntegrationEventLogService, IDisposable
    where TContext : DbContext
{
    private volatile bool _disposedValue;
    private readonly TContext _context;
    private readonly Type[] _eventTypes;

    public EFIntegrationEventLogService(TContext context)
    {
        _context = context;
        _eventTypes = Assembly.Load(Assembly.GetEntryAssembly()!.FullName!)
            .GetTypes()
            .Where(t => t.Name.EndsWith(nameof(IntegrationEvent)))
            .ToArray();
    }

    public async Task<IEnumerable<IIntegrationEventLogEntry>> RetrievePendingEventLogs(Guid transactionId, CancellationToken cancellationToken)
    {
        var result = await _context.Set<IntegrationEventLogEntry>()
            .Where(e => e.TransactionId == transactionId && e.State == EventStateEnum.NotPublished)
            .ToListAsync(cancellationToken);

        if (result.Count != 0)
        {
            return result.OrderBy(o => o.CreationTime)
                .Select(e => e.DeserializeJsonContent(_eventTypes.First(t => t.Name == e.EventTypeShortName)));
        }

        return [];
    }

    public async Task SaveEventAsync(IIntegrationEventLogEntry @event, CancellationToken cancellationToken)
    {
        _context.Set<IIntegrationEventLogEntry>().Add(@event);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveEventAsync(IntegrationEvent @event, IDbContextTransaction transaction, CancellationToken cancellationToken)
    {
        if (transaction == null) 
            throw new ArgumentNullException(nameof(transaction));

        var eventLogEntry = new IntegrationEventLogEntry(@event, transaction.TransactionId);

        _context.Database.UseTransaction(transaction.GetDbTransaction());
        await SaveEventAsync(eventLogEntry,cancellationToken);
    }

    public async Task MarkEventAsPublishedAsync(Guid eventId, CancellationToken cancellationToken)
    {
        await UpdateEventStatus(eventId, EventStateEnum.Published,cancellationToken);
    }

    public async Task MarkEventAsInProgressAsync(Guid eventId, CancellationToken cancellationToken)
    {
        await UpdateEventStatus(eventId, EventStateEnum.InProgress,cancellationToken);
    }

    public async Task MarkEventAsFailedAsync(Guid eventId, CancellationToken cancellationToken)
    {
        await UpdateEventStatus(eventId, EventStateEnum.PublishedFailed,cancellationToken);
    }

    private async Task UpdateEventStatus(Guid eventId, EventStateEnum status, CancellationToken cancellationToken)
    {
        var eventLogEntry = _context.Set<IntegrationEventLogEntry>().Single(ie => ie.EventId == eventId);
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
