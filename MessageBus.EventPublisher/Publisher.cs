using MessageBus.Abstractions;
using MessageBus.Events;
using MessageBus.Example.IntegrationEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace MessageBus.EventPublisher;

public class Publisher : BackgroundService
{
    List<IntegrationEvent> _events = new()
    {
        new OrderCreated(1,"This is order about fruit"),new OrderCreated(2,"This is order about vegetable"),
        new OrderUpdated(3,"This is updated order about fruit"),new OrderUpdated(4,"This is updated order about vegetable"),
        new OrderDeleted(5),new OrderDeleted(6)
    };
    private readonly IServiceProvider _serviceProvider;

    public Publisher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();
            while (!eventBus.IsConnected)
            {
                Console.WriteLine("Publisher is waiting for connection to RabbitMQ");
                await Task.Delay(100);
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var eventsToPublish = await GetEventsToPublish(batchSize: 2);

                    //Instead use Batch Publishing
                    foreach (var @event in eventsToPublish)
                    {
                        await eventBus.PublishAsync(@event);
                        await MarkEventAsPublished(@event.Id);
                    }

                    await Task.Delay(100, stoppingToken);
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

    private async Task<List<IntegrationEvent>> GetEventsToPublish(int batchSize)
    {
        await Task.Delay(100);
        return _events.Take(batchSize).ToList();
    }

    private async Task MarkEventAsPublished(Guid eventId)
    {
        await Task.Delay(100);
        _events.RemoveAll(x => x.Id == eventId);
    }

}
