# MessageBus for Microservices

This repository contians an example of how the messaging can be implemented using the **SHAKA.MessageBus*** libraries in .NET. This are the libraries:
1) https://github.com/ShakaMirtskhulava/SHAKA.MessageBus
2) https://github.com/ShakaMirtskhulava/SHAKA.MessageBus.IntegrationEventLog
3) https://github.com/ShakaMirtskhulava/SHAKA.MessageBus.IntegrationEventLog.EF
4) https://github.com/ShakaMirtskhulava/SHAKA.MessageBus.RabbitMQ

As you well avare **SHAKA.MessageBus** libraries support 2 types of microservice design:
I)Type 1 would be type of an app that implements Event Publishin and Handling logic as well as the business logic in a single .NET exe project.
II)Type 2 would be type of an app that implements Event Publishin, Event Handling and Business Logic implementations all in a different .NET applications.

Let's see how we can utilize SHAKA.MessageBus* libraries to implement the message handling the application of type 1.
First of all we'll need to install the following package:
1)SHAKA.MessageBus.IntegrationEventLog.EF
2)SHAKA.MessageBus.RabbitMQ
As you can see we implement pacakges of speficic implementations of the abstractions for the ORM EF Core and Message Borker RabbitMQ
In the example we'll also have the following packages for general development:
3)Microsoft.AspNetCore.OpenApi
4)Microsoft.EntityFrameworkCore.Tools
5)Swashbuckle.AspNetCore

Since we're using EF core specific package, it means that we'll be using the EF Core for the data access, so we'll have to configure a DbContext in DI Container:

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),opt =>
    {
        opt.EnableRetryOnFailure();
    });
});
```
Now we need to confiugre our message in the DI container, for that we can use the AddRabbitMqEventBus extensions method specifying the details of the RabbitMQ server.
Here we can also register the events and their respective event handlers.
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
Now we need to configure IntegrationEventLog.EF service so that we'll be able to utilize outbox pattern as well as failed message handling logic.
Since our app implements both publishing and handling, we can use an extension method ConfigureEventLogServicesWithPublisher specifing the DBContext that'll be used to 
write an entity updates as well as the event logs in the database.
This extension method also take the options for the publisher, which needs to know about the name of the assembly where all the IntegrationEvents reside.
```csharp
var eventTyepsAssemblyName = typeof(OrderCreated).Assembly.FullName!;
builder.Services.ConfigureEventLogServicesWithPublisher<AppDbContext>(options =>
    {
        options.DelayMs = 1000;
        options.EventsBatchSize = 1000;
        options.FailedMessageChainBatchSize = 100;
        options.EventTyepsAssemblyName = eventTyepsAssemblyName;
    }
);
```
It is necessary for the event classes to implement the IntegrationEvent class as well as the entity classes need to implement the IEntity interface
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

