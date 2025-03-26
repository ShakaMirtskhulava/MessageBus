using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace MessageBus.IntegrationEventLog.EF;

public class UnitOfWork<TContext> where TContext : DbContext
{
    private readonly DbContext _dbContext;
    private IDbContextTransaction? _transaction;
    private IExecutionStrategy? _executionStrategy;

    public UnitOfWork(TContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task BeginTransaction(CancellationToken cancellationToken)
    {
        _transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task RollbackTransaction(CancellationToken cancellation)
    {
        if (_transaction != null)
            await _transaction.RollbackAsync(cancellation).ConfigureAwait(false);
    }

    public async Task CommitTransaction(CancellationToken cancellationToken)
    {
        if (_transaction != null)
            await _transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    public void CreateExecutionStrategy()
    {
        _executionStrategy = _dbContext.Database.CreateExecutionStrategy();
    }

    public async Task<T> ExecuteOnDefaultStarategy<T>(Func<Task<T>> operation)
    {
        if (_executionStrategy == null)
            CreateExecutionStrategy();

        return await _executionStrategy!.ExecuteAsync(operation).ConfigureAwait(false);
    }

    public void Dispose()
    {
        if (_transaction != null)
            _transaction.Dispose();
        _dbContext.Dispose();
    }
}
