using MessageBus.Abstractions;
using MessageBus.Example.IntegrationEvents;

namespace MessageBus.EventHandler.OrderHandlers;

public class OrderDeletedHandler : IIntegrationEventHandler<OrderDeleted>
{
    public Task Handle(OrderDeleted @event)
    {
        Console.WriteLine("Handling the order deleted");
        return Task.CompletedTask;
    }
}