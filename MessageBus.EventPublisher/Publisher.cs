using MessageBus.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MessageBus.EventPublisher;

public class Publisher : BackgroundService
{
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
            while (!eventBus.IsReady)
            {
                Console.WriteLine("Publisher is waiting for connection to RabbitMQ");
                await Task.Delay(100);
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var eventsToPublish = await EventsDbContext.GetEventsToPublish(batchSize: 1000);
                    
                    foreach (var @event in eventsToPublish)
                    {
                        try
                        {
                            await eventBus.PublishAsync(@event);
                            EventsDbContext.MarkEventAsPublished(@event.Id);
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine($"Following event couldn't be published: {@event}");
                            Console.Error.WriteLine($"{DateTime.Now} [ERROR] saw nack or return, ex: {ex}");
                        }
                    }

                    if (!EventsDbContext.Any())
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

}
