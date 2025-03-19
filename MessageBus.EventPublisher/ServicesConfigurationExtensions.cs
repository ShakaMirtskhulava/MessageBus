using MessageBus.Example.IntegrationEvents;
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
        services.ConfigureEFCoreIntegrationEventLogServices<AppDbContext>(eventTyepsAssemblyName);
        services.AddHostedService<Publisher>();

        return services;
    }
}
