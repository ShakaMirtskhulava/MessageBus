using MessageBus.EventHandler.OrderHandlers;
using MessageBus.Example.IntegrationEvents;
using MessageBus.Extensions;
using MessageBus.IntegrationEventLog.EF;
using MessageBus.RabbitMQ;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

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
        .AddSubscription<OrderCreated, OrderCreatedHandler>()
        .AddSubscription<OrderUpdated,OrderUpdatedHandler>()
        .AddSubscription<OrderDeleted, OrderDeletedHandler>();

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"), opt =>
            {
                opt.EnableRetryOnFailure();
            });
        });

        var eventTyepsAssemblyName = typeof(OrderCreated).Assembly.FullName!;
        services.ConfigureEventLogServices<AppDbContext>(eventTyepsAssemblyName);


        return services;
    }
}
