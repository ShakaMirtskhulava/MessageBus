using MessageBus.Abstractions;
using MessageBus.Events;
using MessageBus.IntegrationEventLog.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MessageBus.IntegrationEventLog.Publisher;

public class Publisher : BackgroundService
{
    private readonly int _delay;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _eventTyepsAssemblyName;
    private readonly int _eventsBatchSize;
    private readonly int _failedMessageChainBatchSize;

    public Publisher(IServiceProvider serviceProvider, PublisherOptions options)
    {
        _serviceProvider = serviceProvider;
        _eventTyepsAssemblyName = options.EventTyepsAssemblyName;
        _delay = options.DelayMs;
        _eventsBatchSize = options.EventsBatchSize;
        _failedMessageChainBatchSize = options.FailedMessageChainBatchSize;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();
            var integrationEventService = scope.ServiceProvider.GetRequiredService<IIntegrationEventService>();
            var integrationEventLogService = scope.ServiceProvider.GetRequiredService<IIntegrationEventLogService>();
            while (!eventBus.IsReady)
            {
                Console.WriteLine("Publisher is waiting for connection for the broker to connect");
                await Task.Delay(100);
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    List<IntegrationEvent> eventsToPublish = new();
                    var normalMessages = (await integrationEventService.GetPendingEvents(_eventsBatchSize, _eventTyepsAssemblyName, stoppingToken)).ToList();
                    eventsToPublish.AddRange(normalMessages);
                    var failedMessages = (await integrationEventService.RetriveFailedEventsToRepublish(_failedMessageChainBatchSize, stoppingToken)).ToList();
                    eventsToPublish.AddRange(failedMessages);

                    if (eventsToPublish.Any())
                        Console.WriteLine($"Publisher is going to publish {eventsToPublish.Count} events among which {failedMessages.Count} is failed message");

                    foreach (var @event in eventsToPublish)
                    {
                        try
                        {
                            await integrationEventLogService.MarkEventAsInProgress(@event.Id, stoppingToken);
                            await eventBus.PublishAsync(@event);
                            await integrationEventLogService.MarkEventAsPublished(@event.Id, stoppingToken);
                        }
                        catch (Exception ex)
                        {
                            await integrationEventLogService.MarkEventAsFailed(@event.Id, stoppingToken);
                            Console.Error.WriteLine($"Following event couldn't be published: {@event}");
                            Console.Error.WriteLine($"{DateTime.Now} [ERROR] saw nack or return, ex: {ex}");
                        }
                    }

                    if (!eventsToPublish.Any())
                    {
                        Console.WriteLine($"No events to publish, publisher is waiting for: {_delay}ms");
                        await Task.Delay(_delay, stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("An error occurred while publishing the event: ", ex.Message);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Publisher is stopped, due to the reason: ", ex.Message);
        }
    }
}
