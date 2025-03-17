using MessageBus.IntegrationEventLog.EF.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MessageBus.IntegrationEventLog.EF;

public static class IntegrationLogExtensions
{
    public static void UseIntegrationEventLogs(this ModelBuilder builder)
    {
        builder.Entity<EFCoreIntegrationEventLog>(builder =>
        {
            builder.ToTable(nameof(EFCoreIntegrationEventLog));

            builder.HasKey(e => e.EventId);
        });
    }

    public static void ConfigureEFCoreIntegrationEventLogServices<TContext>(this IServiceCollection services) where TContext : DbContext
    {
        services.AddScoped<IIntegrationEventLogService, EFIntegrationEventLogService<TContext>>();
        services.AddScoped<IUnitOfWork, UnitOfWorkEFCore<TContext>>();
    }

}
