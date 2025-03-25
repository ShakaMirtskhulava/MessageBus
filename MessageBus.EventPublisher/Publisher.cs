using MessageBus.Abstractions;
using MessageBus.Events;
using MessageBus.Example.IntegrationEvents;
using MessageBus.IntegrationEventLog;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MessageBus.EventPublisher;

public class Publisher : BackgroundService
{
    const int DELAY = 1000;
    private readonly IServiceProvider _serviceProvider;

    public Publisher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var eventTyepsAssemblyName = typeof(OrderCreated).Assembly.FullName!;
            using var scope = _serviceProvider.CreateScope();
            var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();
            var integrationEventService = scope.ServiceProvider.GetRequiredService<IIntegrationEventService>();
            var integrationEventLogService = scope.ServiceProvider.GetRequiredService<IIntegrationEventLogService>();
            while (!eventBus.IsReady)
            {
                Console.WriteLine("Publisher is waiting for connection to RabbitMQ");
                await Task.Delay(100);
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var eventsToPublish = (await integrationEventService.GetPendingEvents(1000, eventTyepsAssemblyName, stoppingToken)).ToList();
                    var failedEventsToRepublish = await integrationEventService.RetriveFailedEventsToRepublish(100, stoppingToken);
                    if(failedEventsToRepublish.Any())
                        eventsToPublish.AddRange(failedEventsToRepublish);

                    if (eventsToPublish.Any())
                        Console.WriteLine($"Publisher is going to publish {eventsToPublish.Count()} events, among which {failedEventsToRepublish.Count()} is failed message");

                    foreach (var @event in eventsToPublish)
                    {
                        try
                        {
                            await eventBus.PublishAsync(@event);
                            await integrationEventLogService.MarkEventAsPublished(@event.Id,stoppingToken);
                        }
                        catch (Exception ex)
                        {
                            await integrationEventLogService.MarkEventAsFailed(@event.Id, stoppingToken);
                            Console.Error.WriteLine($"Following event couldn't be published: {@event}");
                            Console.Error.WriteLine($"{DateTime.Now} [ERROR] saw nack or return, ex: {ex}");
                        }
                    }

                    if (!eventsToPublish.Any()){
                        Console.WriteLine($"No events to publish, publisher is waiting for: {DELAY}ms");
                        await Task.Delay(DELAY, stoppingToken);
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
            Console.WriteLine("Publisher is stopped, due to the reason: ",ex.Message);
        }
    }

}
