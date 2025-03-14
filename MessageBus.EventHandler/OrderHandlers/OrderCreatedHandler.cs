using MessageBus.Abstractions;
using MessageBus.Example.IntegrationEvents;

namespace MessageBus.EventHandler.OrderHandlers;

public class OrderCreatedHandler :IIntegrationEventHandler<OrderCreated>
{
    public Task Handle(OrderCreated @event)
    {
        Console.WriteLine("Handling the order created");
        return Task.CompletedTask;
    }
}
