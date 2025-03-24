using MessageBus.Abstractions;
using MessageBus.Example.IntegrationEvents;

namespace MessageBus.EventHandler.OrderHandlers;

public class OrderCreatedHandler :IIntegrationEventHandler<OrderCreated>
{
    static int count = 0;

    public Task Handle(OrderCreated @event)
    {
        if(count == 2)
            throw new Exception("This exception is thrown for testing purposes",new Exception("This inner exception is thrown for the testing urposes"));

        Console.WriteLine("Handling the order created");
        count++;
        return Task.CompletedTask;
    }
}
