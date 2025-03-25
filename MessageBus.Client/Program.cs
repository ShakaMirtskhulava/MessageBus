using MessageBus.Abstractions;
using MessageBus.Client;
using MessageBus.Client.Models;
using MessageBus.Example.IntegrationEvents;
using MessageBus.Extensions;
using MessageBus.IntegrationEventLog.Abstractions;
using MessageBus.IntegrationEventLog.EF;
using MessageBus.RabbitMQ;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),opt =>
    {
        opt.EnableRetryOnFailure();
    });
});
var eventTyepsAssemblyName = typeof(OrderCreated).Assembly.FullName!;

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

builder.Services.ConfigureEventLogServices<AppDbContext>(eventTyepsAssemblyName);
//builder.Services.ConfigureEFCoreEventLogServicesWithPublisher<AppDbContext>(options =>
//    {
//        options.DelayMs = 1000;
//        options.EventsBatchSize = 1000;
//        options.FailedMessageChainBatchSize = 100;
//        options.EventTyepsAssemblyName = eventTyepsAssemblyName;
//    }
//);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPost("/order", async (OrderRequest order,CancellationToken cancellationToken) =>
{
    using var scope = app.Services.CreateScope();
    var integrationEventService = scope.ServiceProvider.GetRequiredService<IIntegrationEventService>();

    Order newOrder = new() { Data = order.Data };
    OrderCreated? orderCreated = new(newOrder.Id, order.Data);
    var @event = await integrationEventService.Add<Order,Guid>(newOrder, orderCreated, cancellationToken);

    return Results.Created($"/order/{newOrder.Id}", newOrder);
})
.WithName("order")
.WithOpenApi();

app.MapPut("/order/{id}", async (Guid id, OrderRequest order, CancellationToken cancellationToken) =>
{
    using var scope = app.Services.CreateScope();
    var integrationEventService = scope.ServiceProvider.GetRequiredService<IIntegrationEventService>();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var targetOrder = await dbContext.Orders.FindAsync(id);
    if(targetOrder is null)
        return Results.NotFound();
    
    targetOrder.Data = order.Data;
    OrderUpdated orderUpdated = new(targetOrder.Id, order.Data);
    var @event = await integrationEventService.Update<Order, Guid>(targetOrder, orderUpdated, cancellationToken);
    return Results.Ok(targetOrder);
});

app.MapPost("/toast", async (ToastRequest toast, CancellationToken cancellationToken) =>
{
    using var scope = app.Services.CreateScope();
    var integrationEventService = scope.ServiceProvider.GetRequiredService<IIntegrationEventService>();

    Toast newToast = new() { Data = toast.Data };
    ToastCreated toastCreated = new(newToast.Id, newToast.Data);
    var @event = await integrationEventService.Add<Toast, int>(newToast, toastCreated, cancellationToken);
})
.WithName("toast")
.WithOpenApi();

app.Run();

public class OrderCreatedEventHandler(ILogger<OrderCreatedEventHandler> logger) : IIntegrationEventHandler<OrderCreated>
{
    public async Task Handle(OrderCreated @event)
    {
        logger.LogInformation("Handling order created event");
        await Task.Delay(100);
    }
}

public class OrderUpdatedEventHandler(ILogger<OrderCreatedEventHandler> logger) : IIntegrationEventHandler<OrderUpdated>
{
    public static int counter = 0;
    public async Task Handle(OrderUpdated @event)
    {
        if (counter == 1)
        {
            counter++;
            throw new Exception("This is very cool exception", new("This is also a very cool inner exception"));
        }
        counter++;

        logger.LogInformation("Handling order updated event");
        await Task.Delay(100);
    }
}

public class ToastCreatedEventHandler(ILogger<ToastCreatedEventHandler> logger) : IIntegrationEventHandler<ToastCreated>
{
    public async Task Handle(ToastCreated @event)
    {
        logger.LogInformation("Handling toast created event");
        await Task.Delay(100);
    }
}