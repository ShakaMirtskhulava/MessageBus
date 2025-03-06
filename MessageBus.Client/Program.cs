using MessageBus.RabbitMQ;
using MessageBus.Extensions;
using MessageBus.Events;
using MessageBus.Abstractions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.AddRabbitMqEventBus("eventbus")
               .AddSubscription<OrderCreated, OrderCreatedEventHandler>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/home", async () =>
{
    OrderCreated orderCreated = new(1, "Very cool order was created");
    var eventBus = app.Services.GetRequiredService<IEventBus>();
    await eventBus.PublishAsync(orderCreated);
})
.WithName("home")
.WithOpenApi();

app.Run();


public record OrderCreated(int OrderId, string data) : IntegrationEvent;

public class OrderCreatedEventHandler(
    ILogger<OrderCreatedEventHandler> logger) :
    IIntegrationEventHandler<OrderCreated>
{
    public async Task Handle(OrderCreated @event)
    {
        logger.LogInformation("Handling integration orderCreated: {IntegrationEventId} - ({@IntegrationEvent})", @event.Id, @event);
        await Task.Delay(100);
        Console.WriteLine(@event.data);
    }
}