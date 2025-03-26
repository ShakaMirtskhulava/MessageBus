using MessageBus.Abstractions;
using MessageBus.Example.IntegrationEvents;

namespace MessageBus.EventHandler.OrderHandlers;

public class OrderUpdatedHandler : IIntegrationEventHandler<OrderUpdated>
{
    static int count = 0;

    public Task Handle(OrderUpdated @event)
    {
        if (count == 2)
        {
            count++;
            throw new Exception("This exception is thrown for testing purposes", new Exception("This inner exception is thrown for the testing urposes"));
        }
        count++;

        Console.WriteLine("Handling the order updated");
        return Task.CompletedTask;
    }
}
