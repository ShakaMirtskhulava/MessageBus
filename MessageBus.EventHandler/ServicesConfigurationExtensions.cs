using MessageBus.EventHandler.OrderHandlers;
using MessageBus.Example.IntegrationEvents;
using MessageBus.Extensions;
using MessageBus.RabbitMQ;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MessageBus.EventHandler;

public static class ServicesConfigurationExtensions
{
    public static IServiceCollection ConfigureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddRabbitMqEventBus(configuration, connectionFactory =>
        {
            connectionFactory.HostName = "localhost";
            connectionFactory.Port = 5672;
            connectionFactory.UserName = "user";
            connectionFactory.Password = "password";
        })
        .AddSubscription<OrderCreated, OrdersEventHandler>();

        return services;
    }
}
