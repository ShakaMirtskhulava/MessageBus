using MessageBus.Abstractions;
using MessageBus.Example.IntegrationEvents;

namespace MessageBus.EventHandler.OrderHandlers;

public class OrderCreatedHandler :IIntegrationEventHandler<OrderCreated>
{
    public async Task Handle(OrderCreated @event)
    {
        Console.WriteLine("Handling the order created");
        await Task.Delay(100);
    }
}
