using MessageBus.Abstractions;
using MessageBus.Client;
using MessageBus.Client.Models;
using MessageBus.Example.IntegrationEvents;
using MessageBus.Extensions;
using MessageBus.IntegrationEventLog;
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
builder.Services.ConfigureEFCoreIntegrationEventLogServices<AppDbContext>(eventTyepsAssemblyName);

builder.AddRabbitMqEventBus(connectionFactory =>
{
    connectionFactory.HostName = "localhost";
    connectionFactory.Port = 5672;
    connectionFactory.UserName = "user";
    connectionFactory.Password = "password";
})
.AddSubscription<OrderCreated, OrderCreatedEventHandler>()
.AddSubscription<ToastCreated, ToastCreatedEventHandler>();

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
    var @event = await integrationEventService.SaveAndPublish<Order,Guid>(newOrder, orderCreated, cancellationToken);
    //var @event = await integrationEventService.Save(toastCreated, cancellationToken);
})
.WithName("order")
.WithOpenApi();

app.MapPost("/toast", async (ToastRequest toast, CancellationToken cancellationToken) =>
{
    using var scope = app.Services.CreateScope();
    var integrationEventService = scope.ServiceProvider.GetRequiredService<IIntegrationEventService>();

    Toast newToast = new() { Data = toast.Data };
    ToastCreated toastCreated = new(newToast.Id, newToast.Data);
    var @event = await integrationEventService.SaveAndPublish<Toast, int>(newToast, toastCreated, cancellationToken);
    //var @event = await integrationEventService.Save(toastCreated, cancellationToken);
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

public class ToastCreatedEventHandler(ILogger<ToastCreatedEventHandler> logger) : IIntegrationEventHandler<ToastCreated>
{
    public async Task Handle(ToastCreated @event)
    {
        logger.LogInformation("Handling toast created event");
        await Task.Delay(100);
    }
}