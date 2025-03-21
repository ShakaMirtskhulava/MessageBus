using MessageBus.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using Microsoft.Extensions.Configuration;

namespace MessageBus.RabbitMQ;

public static class RabbitMqDependencyInjectionExtensions
{
    private const string EventBus = nameof(EventBus);

    public static IEventBusBuilder AddRabbitMqEventBus(this IHostApplicationBuilder builder, Action<ConnectionFactory> configureConnectionFactory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configureConnectionFactory);

        var connectionFactory = new ConnectionFactory();
        configureConnectionFactory(connectionFactory);

        if (string.IsNullOrEmpty(connectionFactory.HostName) || string.IsNullOrEmpty(connectionFactory.UserName) || string.IsNullOrEmpty(connectionFactory.Password))
            throw new InvalidOperationException("HostName, UserName, and Password must be set for ConnectionFactory");

        builder.Services.AddSingleton(connectionFactory);

        builder.Services.Configure<EventBusOptions>(builder.Configuration.GetSection(EventBus));

        builder.Services.AddSingleton<IEventBus, RabbitMQEventBus>();
        builder.Services.AddSingleton<IHostedService>(sp => (RabbitMQEventBus)sp.GetRequiredService<IEventBus>());

        return new EventBusBuilder(builder.Services);
    }

    public static IEventBusBuilder AddRabbitMqEventBus(this IServiceCollection serviceProvider,IConfiguration configuration, Action<ConnectionFactory> configureConnectionFactory)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(configureConnectionFactory);

        var connectionFactory = new ConnectionFactory();
        configureConnectionFactory(connectionFactory);

        if (string.IsNullOrEmpty(connectionFactory.HostName) || string.IsNullOrEmpty(connectionFactory.UserName) || string.IsNullOrEmpty(connectionFactory.Password))
            throw new InvalidOperationException("HostName, UserName, and Password must be set for ConnectionFactory");

        serviceProvider.AddSingleton(connectionFactory);

        serviceProvider.Configure<EventBusOptions>(configuration.GetSection(EventBus));

        serviceProvider.AddSingleton<IEventBus, RabbitMQEventBus>();
        serviceProvider.AddSingleton<IHostedService>(sp => (RabbitMQEventBus)sp.GetRequiredService<IEventBus>());

        return new EventBusBuilder(serviceProvider);
    }


    private class EventBusBuilder(IServiceCollection services) : IEventBusBuilder
    {
        public IServiceCollection Services => services;
    }
}
