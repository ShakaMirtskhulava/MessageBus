# MessageBus for Microservices

This repository contains an example of how messaging can be implemented using the **SHAKA.MessageBus** libraries in .NET. Below are the core libraries used:

1. [SHAKA.MessageBus](https://github.com/ShakaMirtskhulava/SHAKA.MessageBus)
2. [SHAKA.MessageBus.IntegrationEventLog](https://github.com/ShakaMirtskhulava/SHAKA.MessageBus.IntegrationEventLog)
3. [SHAKA.MessageBus.IntegrationEventLog.EF](https://github.com/ShakaMirtskhulava/SHAKA.MessageBus.IntegrationEventLog.EF)
4. [SHAKA.MessageBus.RabbitMQ](https://github.com/ShakaMirtskhulava/SHAKA.MessageBus.RabbitMQ)

## Microservice Design Types

SHAKA.MessageBus supports two types of microservice architectures:

1. **Type 1:** A single .NET application that implements event publishing, handling, and business logic.
2. **Type 2:** Separate .NET applications for event publishing, event handling, and business logic.

---

## Implementing Message Handling in a Type 1 Application

In this setup, all the messaging logic, including publishing and handling, is implemented in a single application.

### Required Packages

We need to install the following NuGet packages:

- `SHAKA.MessageBus.IntegrationEventLog.EF`
- `SHAKA.MessageBus.RabbitMQ`
- Additional general development packages:
  - `Microsoft.AspNetCore.OpenApi`
  - `Microsoft.EntityFrameworkCore.Tools`
  - `Swashbuckle.AspNetCore`

### Configuring the Database Context

Since we are using EF Core for data access, we need to configure a `DbContext` in the DI container:

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), opt =>
    {
        opt.EnableRetryOnFailure();
    });
});
```

### Configuring the Message Bus

To use RabbitMQ as our event bus, we configure it in the DI container and register event subscriptions:

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

### Configuring the Event Log Service

The `IntegrationEventLog.EF` service enables the outbox pattern and failed message handling. Since this app both publishes and handles events, we use:

```csharp
var eventTypesAssemblyName = typeof(OrderCreated).Assembly.FullName!;
builder.Services.ConfigureEventLogServicesWithPublisher<AppDbContext>(options =>
{
    options.DelayMs = 1000;
    options.EventsBatchSize = 1000;
    options.FailedMessageChainBatchSize = 100;
    options.EventTyepsAssemblyName = eventTypesAssemblyName;
});
```

### Implementing Event Classes

Event classes must implement `IntegrationEvent`, and entity classes must implement `IEntity<T>`:

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

Handlers must implement `IIntegrationEventHandler<T>`:

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

### Creating an Order

We define an API endpoint that creates an order, writes the entity and event log in the database, and publishes the event:

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

## Implementing Message Handling in a Type 2 Application

In this setup, we have three separate projects:

1. **Business Logic Project** - Handles data storage and event logging.
2. **Event Publisher** - Reads from the event log and publishes events.
3. **Event Handler** - Listens for events and processes them.

### Required Packages

#### Business Logic Project:
- `SHAKA.MessageBus.RabbitMQ`
- `SHAKA.MessageBus.IntegrationEventLog.EF`

#### Event Publisher:
- `SHAKA.MessageBus.RabbitMQ`
- `SHAKA.MessageBus.IntegrationEventLog.EF`

#### Event Handler:
- `SHAKA.MessageBus.RabbitMQ`
- `SHAKA.MessageBus.IntegrationEventLog.EF`

### Business Logic Configuration

We configure the database context and RabbitMQ in our business logic project:

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), opt =>
    {
        opt.EnableRetryOnFailure();
    });
});

builder.AddRabbitMqEventBus(connectionFactory =>
{
    connectionFactory.HostName = "localhost";
    connectionFactory.Port = 5672;
    connectionFactory.UserName = "user";
    connectionFactory.Password = "password";
});
```

Since we are only logging events here, we do **not** configure a publisher:

```csharp
var eventTypesAssemblyName = typeof(OrderCreated).Assembly.FullName!;
builder.Services.ConfigureEventLogServices<AppDbContext>(eventTypesAssemblyName);
```

### Event Handling Configuration

```csharp
services.AddRabbitMqEventBus(configuration, connectionFactory =>
{
    connectionFactory.HostName = "localhost";
    connectionFactory.Port = 5672;
    connectionFactory.UserName = "user";
    connectionFactory.Password = "password";
})
.AddSubscription<OrderCreated, OrderCreatedHandler>();
```

### Event Publisher Configuration

```csharp
services.ConfigureEventLogServicesWithPublisher<AppDbContext>(options =>
{
    options.DelayMs = 1000;
    options.EventsBatchSize = 1000;
    options.FailedMessageChainBatchSize = 100;
    options.EventTyepsAssemblyName = eventTypesAssemblyName;
});
```

### Final Notes

Ensure database migrations are generated and applied. Start the **Event Handler** before the **Event Publisher**, as event handlers create queues required by the publisher.

