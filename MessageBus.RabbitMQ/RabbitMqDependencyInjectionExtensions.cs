using MessageBus.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using Microsoft.Extensions.Configuration;

namespace MessageBus.RabbitMQ;

public static class RabbitMqDependencyInjectionExtensions
{
    private const string SectionName = "EventBus";

    public static IEventBusBuilder AddRabbitMqEventBus(this IHostApplicationBuilder builder, string connectionName)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var connectionFactory = new ConnectionFactory
        {
            HostName = "localhost",
            Port = 5672,
            UserName = "user",
            Password = "password"
        };

        builder.Services.AddSingleton(connectionFactory);

        builder.Services.Configure<EventBusOptions>(builder.Configuration.GetSection(SectionName));

        builder.Services.AddSingleton<IEventBus, RabbitMQEventBus>();
        builder.Services.AddSingleton<IHostedService>(sp => (RabbitMQEventBus)sp.GetRequiredService<IEventBus>());

        return new EventBusBuilder(builder.Services);
    }

    private class EventBusBuilder(IServiceCollection services) : IEventBusBuilder
    {
        public IServiceCollection Services => services;
    }
}
