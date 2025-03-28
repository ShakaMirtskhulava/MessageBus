# MessageBus for Microservices

This repository contains an example of how messaging can be implemented using the **SHAKA.MessageBus** libraries in .NET.

## SHAKA.MessageBus Libraries
The following libraries are used:

1. [SHAKA.MessageBus](https://github.com/ShakaMirtskhulava/SHAKA.MessageBus)
2. [SHAKA.MessageBus.IntegrationEventLog](https://github.com/ShakaMirtskhulava/SHAKA.MessageBus.IntegrationEventLog)
3. [SHAKA.MessageBus.IntegrationEventLog.EF](https://github.com/ShakaMirtskhulava/SHAKA.MessageBus.IntegrationEventLog.EF)
4. [SHAKA.MessageBus.RabbitMQ](https://github.com/ShakaMirtskhulava/SHAKA.MessageBus.RabbitMQ)

## Microservice Design Approaches
SHAKA.MessageBus supports two types of microservice designs:

1. **Type 1** - An application that implements Event Publishing, Event Handling, and Business Logic in a single .NET executable project.
2. **Type 2** - An application where Event Publishing, Event Handling, and Business Logic are implemented in separate .NET applications.

## Implementing Message Handling in a Type 1 Application
### Required Packages
To implement messaging, install the following NuGet packages:

1. `SHAKA.MessageBus.IntegrationEventLog.EF`
2. `SHAKA.MessageBus.RabbitMQ`

These packages provide specific implementations for:
- **ORM**: `SHAKA.MessageBus.IntegrationEventLog.EF` for Entity Framework Core.
- **Message Broker**: `SHAKA.MessageBus.RabbitMQ` for RabbitMQ.

Additional dependencies for general development:

3. `Microsoft.AspNetCore.OpenApi`
4. `Microsoft.EntityFrameworkCore.Tools`
5. `Swashbuckle.AspNetCore`

### Configuring EF Core DbContext
Since we're using an EF Core-specific package, configure a `DbContext` in the Dependency Injection (DI) container:

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), opt =>
    {
        opt.EnableRetryOnFailure();
    });
});
```

### Configuring Message Bus
Register the RabbitMQ event bus in the DI container using `AddRabbitMqEventBus`. Here, we also register events and their respective handlers:

```csharp
builder.AddRabbitMqEventBus(connectionFactory =>
{
    connectionFactory.HostName = "localhost";
    connectionFactory.Port = 5672;
    connectionFactory.UserName = "user";
    connectionFactory.Password = "password";
})
.AddSubscription<OrderCreated, OrderCreatedEventHandler>()
.AddSubscription<OrderUpdated, OrderUpdatedEventHandler>()
.AddSubscription<ToastCreated, ToastCreatedEventHandler>();
```

### Configuring Integration Event Log for Outbox Pattern
To enable the **outbox pattern** and failed message handling logic, configure `IntegrationEventLog.EF`. Since our app handles both event publishing and handling, use `ConfigureEventLogServicesWithPublisher`, specifying the `DbContext` used for storing entity updates and event logs.

```csharp
var eventTypesAssemblyName = typeof(OrderCreated).Assembly.FullName!;
builder.Services.ConfigureEventLogServicesWithPublisher<AppDbContext>(options =>
{
    options.DelayMs = 1000;
    options.EventsBatchSize = 1000;
    options.FailedMessageChainBatchSize = 100;
    options.EventTypesAssemblyName = eventTypesAssemblyName;
});
```

### Defining Event and Entity Classes
Event classes must implement `IntegrationEvent`, while entity classes must implement `IEntity<T>`:

```csharp
public class Order : IEntity<Guid>
{
    [Key]
    public Guid Id { get; set; } = new();
    
    [Required]
    public required string Data { get; set; }
}

public record OrderCreated : IntegrationEvent
{
    public string Data { get; set; }
    
    public OrderCreated(Guid orderId, string data) : base(orderId)
    {
        Data = data;
    }
}
```

### Implementing Event Handlers
Event handlers must implement `IIntegrationEventHandler<T>`:

```csharp
public class OrderCreatedEventHandler(ILogger<OrderCreatedEventHandler> logger) : IIntegrationEventHandler<OrderCreated>
{
    public async Task Handle(OrderCreated @event)
    {
        logger.LogInformation("Handling order created event");
        await Task.Delay(100);
    }
}
```

### Running Database Migrations
Since EF Core is used, the necessary tables (`IntegrationEventLogs`, `FailedMessages`, and `FailedMessageChains`) must be created. Generate a migration after configuring the DI container to create the required schema.

### Creating an Order and Publishing an Event
The following API endpoint creates an order and publishes an event:

```csharp
app.MapPost("/order", async (OrderRequest order, CancellationToken cancellationToken) =>
{
    using var scope = app.Services.CreateScope();
    var integrationEventService = scope.ServiceProvider.GetRequiredService<IIntegrationEventService>();

    Order newOrder = new() { Data = order.Data };
    OrderCreated orderCreated = new(newOrder.Id, order.Data);
    var @event = await integrationEventService.Add<Order, Guid>(newOrder, orderCreated, cancellationToken);

    return Results.Created($"/order/{newOrder.Id}", newOrder);
})
.WithName("order")
.WithOpenApi();
```

---
This guide provides a structured approach to implementing messaging using **SHAKA.MessageBus** libraries in a Type 1 microservice architecture. 🚀

## Implementing Message Handling in a Type 1 Application
So in this case we'll have 3 project, 1 will be a presentation layer and the other 2 is are the worker projects EventPublisher and event handler.
Packages for API:



