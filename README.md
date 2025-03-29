# MessageBus for Microservices

This repository contains an example of how messaging can be implemented using the **SHAKA.MessageBus** libraries in .NET. Below are the libraries used:

1. [SHAKA.MessageBus](https://github.com/ShakaMirtskhulava/SHAKA.MessageBus)
2. [SHAKA.MessageBus.IntegrationEventLog](https://github.com/ShakaMirtskhulava/SHAKA.MessageBus.IntegrationEventLog)
3. [SHAKA.MessageBus.IntegrationEventLog.EF](https://github.com/ShakaMirtskhulava/SHAKA.MessageBus.IntegrationEventLog.EF)
4. [SHAKA.MessageBus.RabbitMQ](https://github.com/ShakaMirtskhulava/SHAKA.MessageBus.RabbitMQ)

## Microservice Design Approaches

SHAKA.MessageBus libraries support two types of microservice design:

### Type 1: Single Application
- The app implements Event Publishing, Event Handling, and Business Logic within a single .NET executable project.

### Type 2: Separate Services
- Event Publishing, Event Handling, and Business Logic are implemented in separate .NET applications.

---

## Implementing Message Handling in a Type 1 Application

To utilize **SHAKA.MessageBus** libraries for message handling, we need to install the following packages:

### Required Packages
1. `SHAKA.MessageBus.IntegrationEventLog.EF`
2. `SHAKA.MessageBus.RabbitMQ`
3. `Microsoft.AspNetCore.OpenApi`
4. `Microsoft.EntityFrameworkCore.Tools`
5. `Swashbuckle.AspNetCore`

Since we are using an EF Core-specific package, we must configure a `DbContext` in the DI container:

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), opt =>
    {
        opt.EnableRetryOnFailure();
    });
});
```

### Configuring RabbitMQ in DI Container

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

### Configuring Integration Event Log Service

```csharp
var eventTyepsAssemblyName = typeof(OrderCreated).Assembly.FullName!;
builder.Services.ConfigureEventLogServicesWithPublisher<AppDbContext>(options =>
{
    options.DelayMs = 1000;
    options.EventsBatchSize = 1000;
    options.FailedMessageChainBatchSize = 100;
    options.EventTyepsAssemblyName = eventTyepsAssemblyName;
});
```

### Event and Entity Classes

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

### Event Handlers

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

### Creating an Order Endpoint

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

In this approach, we have three projects:
1. **Business Logic Project** - Implements core business logic.
2. **Event Publisher** - Responsible for publishing events.
3. **Event Handler** - Listens to and processes events.

### Required Packages
#### Business Logic Project
- `SHAKA.MessageBus.RabbitMQ`
- `SHAKA.MessageBus.IntegrationEventLog.EF`

#### Event Publisher
- `SHAKA.MessageBus.RabbitMQ`
- `SHAKA.MessageBus.IntegrationEventLog.EF`

#### Event Handler
- `SHAKA.MessageBus.IntegrationEventLog.EF`
- `SHAKA.MessageBus.RabbitMQ`

### Configuring Business Logic Project

#### Configuring `DbContext`
```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), opt =>
    {
        opt.EnableRetryOnFailure();
    });
});
```

#### Configuring RabbitMQ
```csharp
builder.AddRabbitMqEventBus(connectionFactory =>
{
    connectionFactory.HostName = "localhost";
    connectionFactory.Port = 5672;
    connectionFactory.UserName = "user";
    connectionFactory.Password = "password";
});
```

#### Configuring Event Log Service
```csharp
var eventTyepsAssemblyName = typeof(OrderCreated).Assembly.FullName!;
builder.Services.ConfigureEventLogServices<AppDbContext>(eventTyepsAssemblyName);
```

### Configuring Event Handler Project

```csharp
services.AddRabbitMqEventBus(configuration, connectionFactory =>
{
    connectionFactory.HostName = "localhost";
    connectionFactory.Port = 5672;
    connectionFactory.UserName = "user";
    connectionFactory.Password = "password";
})
.AddSubscription<OrderCreated, OrderCreatedHandler>()
.AddSubscription<OrderUpdated, OrderUpdatedHandler>()
.AddSubscription<OrderDeleted, OrderDeletedHandler>();
```

#### Implementing an Event Handler
```csharp
public class OrderCreatedHandler : IIntegrationEventHandler<OrderCreated>
{
    public Task Handle(OrderCreated @event)
    {
        Console.WriteLine("Handling the order created");
        return Task.CompletedTask;
    }
}
```

### Configuring Event Publisher Project

```csharp
services.AddRabbitMqEventBus(configuration, connectionFactory =>
{
    connectionFactory.HostName = "localhost";
    connectionFactory.Port = 5672;
    connectionFactory.UserName = "user";
    connectionFactory.Password = "password";
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
- Migrations are generated and the database is created.
- All three projects use the same database.
- Event Handlers start **before** Event Publishers to avoid failures due to missing queues.

