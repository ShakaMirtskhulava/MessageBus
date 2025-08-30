# MessageBus for Microservices

This repository contains an example of how messaging can be implemented using the **SHAKA.MessageBus** libraries in .NET. Below are the relevant libraries:

1. [SHAKA.MessageBus](https://github.com/ShakaMirtskhulava/SHAKA.MessageBus)
2. [SHAKA.MessageBus.IntegrationEventLog](https://github.com/ShakaMirtskhulava/SHAKA.MessageBus.IntegrationEventLog)
3. [SHAKA.MessageBus.IntegrationEventLog.EF](https://github.com/ShakaMirtskhulava/SHAKA.MessageBus.IntegrationEventLog.EF)
4. [SHAKA.MessageBus.RabbitMQ](https://github.com/ShakaMirtskhulava/SHAKA.MessageBus.RabbitMQ)

## Microservice Design Approaches

SHAKA.MessageBus libraries support two types of microservice design:

1. **Type 1:** The application implements event publishing, event handling, and business logic within a single .NET executable project.
2. **Type 2:** The application separates event publishing, event handling, and business logic into different .NET applications.

---

# Implementing Message Handling in a Type 1 Application

In a Type 1 application, all event handling and publishing occur within the same application. Let's go through the required steps.

### Required Packages

To get started, install the following NuGet packages:

- `SHAKA.MessageBus.IntegrationEventLog.EF`
- `SHAKA.MessageBus.RabbitMQ`

Additionally, for general development, you may also need:

- `Microsoft.AspNetCore.OpenApi`
- `Microsoft.EntityFrameworkCore.Tools`
- `Swashbuckle.AspNetCore`

### Configuring the Database Context

Since we are using Entity Framework Core, we need to configure the `DbContext` in the dependency injection container:

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), opt =>
    {
        opt.EnableRetryOnFailure();
    });
});
```

### Configuring RabbitMQ and Event Handlers

Now, we configure RabbitMQ as our message broker and register our event handlers:

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

### Configuring the Integration Event Log

We configure the `IntegrationEventLog.EF` service to enable the outbox pattern and failed message handling logic:

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

### Implementing Event and Entity Classes

Entities must implement the `IEntity` interface, and events must derive from `IntegrationEvent`:

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
    public string Data { get; init; }

    public OrderCreated(Guid orderId, string data) : base(orderId)
    {
        Data = data;
    }

    [JsonConstructor]
    public OrderCreated(Guid id, string data, DateTime creationDate, string? correlationId, object entityId) : base(entityId)
    {
        Id = id;
        Data = data;
        CreationDate = creationDate;
        CorrelationId = correlationId;
        EntityId = entityId;
    }
}
```

Do not forget to give the event a JsonConstructor, which'll be used by the Json Serializer to deserialize the Contnet. Alternatively
you can use the primary constructor and it'll work as well.

### Implementing Event Handlers

Event handlers must implement the `IIntegrationEventHandler<T>` interface:

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

### Creating an Order and Publishing an Event

Now, we define an endpoint that creates an order and publishes an event:

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

# Implementing Message Handling in a Type 2 Application

In a Type 2 application, event publishing, event handling, and business logic are implemented in separate projects. Here, we will have three projects:

1. **Business Logic Project** - Implements core application logic.
2. **Event Publisher** - Reads integration event logs and publishes messages.
3. **Event Handler** - Listens to messages and processes them.

### Required Packages

| Component | Packages |
|-----------|----------|
| Business Logic | SHAKA.MessageBus.RabbitMQ, SHAKA.MessageBus.IntegrationEventLog.EF |
| Event Publisher | SHAKA.MessageBus.RabbitMQ, SHAKA.MessageBus.IntegrationEventLog.EF |
| Event Handler | SHAKA.MessageBus.RabbitMQ, SHAKA.MessageBus.IntegrationEventLog.EF |

### Configuring the Business Logic Project

Set up the `DbContext` and RabbitMQ event bus:

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

Use `ConfigureEventLogServices()` to register the event log service without a publisher:

```csharp
var eventTypesAssemblyName = typeof(OrderCreated).Assembly.FullName!;
builder.Services.ConfigureEventLogServices<AppDbContext>(eventTypesAssemblyName);
```

### Configuring the Event Handler Project

Set up RabbitMQ and register event handlers as well as configure the event logs and event services using ConfigureEventLogServices method,
since this app has nothing to do with publishing we won't use ConfigureEventLogServicesWithPublisher method:

```csharp
services.AddRabbitMqEventBus(configuration, connectionFactory =>
{
    connectionFactory.HostName = "localhost";
    connectionFactory.Port = 5672;
    connectionFactory.UserName = "user";
    connectionFactory.Password = "password";
})
.AddSubscription<OrderCreated, OrderCreatedHandler>();

services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"), opt =>
    {
        opt.EnableRetryOnFailure();
    });
});

var eventTyepsAssemblyName = typeof(OrderCreated).Assembly.FullName!;
services.ConfigureEventLogServices<AppDbContext>(eventTyepsAssemblyName);
```

Here we also define a event handlers
```csharp
public class OrderCreatedHandler :IIntegrationEventHandler<OrderCreated>
{
    public Task Handle(OrderCreated @event)
    {
        Console.WriteLine("Handling the order created");
        return Task.CompletedTask;
    }
}
```

### Configuring the Event Publisher Project

Configure the publisher to read event logs and publish messages:

```csharp
services.AddRabbitMqEventBus(configuration,connectionFactory =>
{
    connectionFactory.HostName = "localhost";
    connectionFactory.Port = 5672;
    connectionFactory.UserName = "user";
    connectionFactory.Password = "password";
});

services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"), opt =>
    {
        opt.EnableRetryOnFailure();
    });
});
var eventTyepsAssemblyName = typeof(OrderCreated).Assembly.FullName!;

services.ConfigureEventLogServicesWithPublisher<AppDbContext>(options =>
{
    options.DelayMs = 1000;
    options.EventsBatchSize = 1000;
    options.FailedMessageChainBatchSize = 100;
    options.EventTyepsAssemblyName = eventTyepsAssemblyName;
});
```

### Final Notes

Ensure that:
- Migrations are generated, and the database is created.
- Event handlers run before event publishers to create necessary queues in RabbitMQ.

This setup ensures a robust, scalable, and reliable message-handling system for microservices.

