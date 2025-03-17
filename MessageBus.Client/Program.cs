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
builder.Services.ConfigureEFCoreIntegrationEventLogServices<AppDbContext>();

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
    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
    var integrationEventLogService = scope.ServiceProvider.GetRequiredService<IIntegrationEventLogService>();

    Order newOrder = new() { Data = order.Data };
    var addedOrder = await dbContext.Orders.AddAsync(newOrder);
    OrderCreated? orderCreated = null;
    var orderCreateEventLog = await unitOfWork.ExecuteOnDefaultStarategy(async () =>
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            orderCreated = new(addedOrder.Entity.Id, addedOrder.Entity.Data);
            await integrationEventLogService.SaveEvent<EFCoreIntegrationEventLog>(orderCreated, cancellationToken);
            await transaction.CommitAsync();
            return orderCreated;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            await transaction.RollbackAsync();
            throw;
        }
    });

    var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();
    try
    {
        await integrationEventLogService.MarkEventAsInProgress(orderCreateEventLog.Id, cancellationToken);
        await eventBus.PublishAsync(orderCreated!);
        await integrationEventLogService.MarkEventAsPublished(orderCreateEventLog.Id, cancellationToken);
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
        await integrationEventLogService.MarkEventAsFailed(orderCreateEventLog.Id, cancellationToken);
    }

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