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
var eventTyepsAssemblyName = typeof(OrderCreated).Assembly.FullName;
builder.Services.ConfigureEFCoreIntegrationEventLogServices<AppDbContext>(eventTyepsAssemblyName);

builder.AddRabbitMqEventBus(connectionFactory =>
{
    connectionFactory.HostName = "localhost";
    connectionFactory.Port = 5672;
    connectionFactory.UserName = "user";
    connectionFactory.Password = "password";
})
.AddSubscription<OrderCreated, OrderCreatedEventHandler>();

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
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var integrationEventService = scope.ServiceProvider.GetRequiredService<IIntegrationEventService>();

    Order newOrder = new() { Data = order.Data };
    var addedOrder = await dbContext.Orders.AddAsync(newOrder);
    OrderCreated? orderCreated = new(addedOrder.Entity.Id, addedOrder.Entity.Data);
    //var @event = await integrationEventService.SaveAndPublish(orderCreated, cancellationToken);
    var @event = await integrationEventService.Save(orderCreated, cancellationToken);
})
.WithName("order")
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