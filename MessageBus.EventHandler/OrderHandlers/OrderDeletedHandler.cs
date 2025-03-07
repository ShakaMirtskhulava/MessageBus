using MessageBus.Abstractions;
using MessageBus.Example.IntegrationEvents;

namespace MessageBus.EventHandler.OrderHandlers;

public class OrderDeletedHandler : IIntegrationEventHandler<OrderDeleted>
{
    public async Task Handle(OrderDeleted @event)
    {
        Console.WriteLine("Handling the order deleted");
        await Task.Delay(100);
    }
}