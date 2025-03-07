using MessageBus.Abstractions;
using MessageBus.Example.IntegrationEvents;

namespace MessageBus.EventHandler.OrderHandlers;

public class OrderUpdatedHandler : IIntegrationEventHandler<OrderUpdated>
{
    public async Task Handle(OrderUpdated @event)
    {
        Console.WriteLine("Handling the order updated");
        await Task.Delay(100);
    }
}
