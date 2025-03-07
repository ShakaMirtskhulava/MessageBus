using MessageBus.Abstractions;
using MessageBus.Events;
using MessageBus.Example.IntegrationEvents;

namespace MessageBus.EventHandler.OrderHandlers;

public class OrdersEventHandler :
    IIntegrationEventHandler<OrderCreated>,
    IIntegrationEventHandler<OrderDeleted>,
    IIntegrationEventHandler<OrderUpdated>

{
    public async Task Handle(OrderCreated @event)
    {
        Console.WriteLine("Handling the order created");
        await Task.Delay(100);
    }

    public async Task Handle(OrderDeleted @event)
    {
        Console.WriteLine("Handling the order deleted)");
        await Task.Delay(100);
    }

    public async Task Handle(OrderUpdated @event)
    {
        Console.WriteLine("Handling the order updated)");
        await Task.Delay(100);
    }

    async Task IIntegrationEventHandler.Handle(IntegrationEvent @event)
    {
        Console.WriteLine("Handling the event: {@event}", @event);

        switch (@event)
        {
            case OrderCreated orderCreated:
                await Handle(orderCreated);
                break;
            case OrderDeleted orderDeleted:
                await Handle(orderDeleted);
                break;
            case OrderUpdated orderUpdated:
                await Handle(orderUpdated);
                break;
            default:
                throw new ArgumentException($"Event type {@event.GetType().Name} is not supported.");
        }
    }
}
