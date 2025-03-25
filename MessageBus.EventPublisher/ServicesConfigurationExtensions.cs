using MessageBus.Example.IntegrationEvents;
using MessageBus.IntegrationEventLog;
using MessageBus.IntegrationEventLog.EF;
using MessageBus.RabbitMQ;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MessageBus.EventPublisher;

public static class ServicesConfigurationExtensions
{
    public static IServiceCollection ConfigureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddRabbitMqEventBus(configuration,connectionFactory =>
        {
            connectionFactory.HostName = "localhost";
            connectionFactory.Port = 5672;
            connectionFactory.UserName = "user";
            connectionFactory.Password = "password";
        });

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"), opt =>
            {
                opt.EnableRetryOnFailure();
            });
        });
        var eventTyepsAssemblyName = typeof(OrderCreated).Assembly.FullName!;

        PublisherOptions options = new(delayMs:1000, eventsBatchSize: 1000, failedMessageChainBatchSize: 100,eventTyepsAssemblyName: eventTyepsAssemblyName);
        services.ConfigureEventLogServicesWithPublisher<AppDbContext>(options);

        return services;
    }
}
