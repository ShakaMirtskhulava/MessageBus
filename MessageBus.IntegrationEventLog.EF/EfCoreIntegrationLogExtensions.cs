using MessageBus.Abstractions;
using MessageBus.IntegrationEventLog.EF.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MessageBus.IntegrationEventLog.EF;

public static class EfCoreIntegrationLogExtensions
{
    const string INTEGRATION_EVNET_LOG_TABLE_NAME = "IntegrationEventLogs";

    public static void UseIntegrationEventLogs(this ModelBuilder builder)
    {
        builder.Entity<EFCoreIntegrationEventLog>(builder =>
        {
            builder.ToTable(INTEGRATION_EVNET_LOG_TABLE_NAME);

            builder.HasKey(e => e.EventId);
        });
    }

    public static void ConfigureEFCoreIntegrationEventLogServices<TContext>(this IServiceCollection services, string eventTyepsAssemblyName) where TContext : DbContext
    {
        services.AddScoped<IIntegrationEventLogService, EFIntegrationEventLogService<TContext>>();
        services.AddScoped<IUnitOfWork, UnitOfWorkEFCore<TContext>>();
        services.AddScoped<IIntegrationEventService, EFCoreIntegrationEventService<TContext>>(
            provider =>
            { 
                var scope = provider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var integrationEventLogService = scope.ServiceProvider.GetRequiredService<IIntegrationEventLogService>();
                var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();

                return new EFCoreIntegrationEventService<TContext>(dbContext, unitOfWork, integrationEventLogService, eventBus, eventTyepsAssemblyName);
            }
        );
    }

}
