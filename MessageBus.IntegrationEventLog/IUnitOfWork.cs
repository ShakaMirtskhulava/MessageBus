namespace MessageBus.IntegrationEventLog;

public interface IUnitOfWork
{
    Task BeginTransaction(CancellationToken cancellationToken);
    Task RollbackTransaction(CancellationToken cancellationToken);
    Task CommitTransaction(CancellationToken cancellationToken);
    Task<T> ExecuteOnDefaultStarategy<T>(Func<Task<T>> operation);
}