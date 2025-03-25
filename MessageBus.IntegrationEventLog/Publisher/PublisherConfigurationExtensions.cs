using Microsoft.Extensions.DependencyInjection;

namespace MessageBus.IntegrationEventLog.Publisher;

public static class PublisherConfigurationExtensions
{
    public static void ConfigurePublisher(this IServiceCollection services, Action<PublisherOptions> optionsAction)
    {
        ArgumentNullException.ThrowIfNull(optionsAction);

        PublisherOptions options = new();
        optionsAction(options);

        services.AddHostedService<Publisher>(provder => new(provder, options));
    }
}
