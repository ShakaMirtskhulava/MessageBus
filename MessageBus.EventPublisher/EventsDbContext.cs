using MessageBus.Events;
using MessageBus.Example.IntegrationEvents;

namespace MessageBus.EventPublisher;

public static class EventsDbContext
{
    public static List<IntegrationEvent> _events = new()
    {
        new OrderCreated(new(),"This is order about fruit"),new OrderCreated(new(),"This is order about vegetable"),
        new OrderUpdated(new(),"This is updated order about fruit"),new OrderUpdated(new(),"This is updated order about vegetable"),
        new OrderDeleted(new()),new OrderDeleted(new())
    };

    public static async Task<List<IntegrationEvent>> GetEventsToPublish(int batchSize)
    {
        await Task.Delay(Random.Shared.Next(50, 100));
        return _events.Take(batchSize).ToList();
    }

    public static void MarkEventAsPublished(Guid eventId)
    {
        _events.RemoveAll(x => x.Id == eventId);
    }

    public static bool Any()
    {
        return _events.Any();
    }

    public static int Count()
    {
        return _events.Count;
    }

}