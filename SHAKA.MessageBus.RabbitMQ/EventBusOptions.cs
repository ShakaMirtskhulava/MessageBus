namespace MessageBus.RabbitMQ;

public class EventBusOptions
{
    public int RetryCount { get; set; } = 10;
}
