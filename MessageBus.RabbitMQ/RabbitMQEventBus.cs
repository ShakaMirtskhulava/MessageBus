using MessageBus.Abstractions;
using MessageBus.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace MessageBus.RabbitMQ;

public sealed class RabbitMQEventBus(
    ILogger<RabbitMQEventBus> logger,
    IServiceProvider serviceProvider,
    IOptions<EventBusOptions> options,
    IOptions<EventBusSubscriptionInfo> subscriptionOptions
    ) : IEventBus, IDisposable, IHostedService
{
    private readonly ResiliencePipeline _pipeline = CreateResiliencePipeline(options.Value.RetryCount);
    private readonly EventBusSubscriptionInfo _subscriptionInfo = subscriptionOptions.Value;
    private IConnection? _rabbitMQConnection;
    private IChannel? _consumerChannel;

    public bool IsReady => _rabbitMQConnection?.IsOpen ?? false;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = Task.Factory.StartNew(async () =>
        {
            try
            {
                logger.LogInformation("Starting RabbitMQ connection on a background thread");

                var connectionFactory = serviceProvider.GetService<ConnectionFactory>() ?? throw new InvalidOperationException("ConnectionFactory is not registered");
                _rabbitMQConnection = await connectionFactory.CreateConnectionAsync();

                if (!_rabbitMQConnection.IsOpen)
                    return;

                _consumerChannel = await _rabbitMQConnection.CreateChannelAsync();

                _consumerChannel.CallbackExceptionAsync += (sender, ea) =>
                {
                    logger.LogWarning(ea.Exception, "Error with RabbitMQ consumer channel");
                    return Task.CompletedTask;
                };

                //If it is an event publisher that's running this code
                //This'll be empty
                foreach (var (eventFullName, eventType) in _subscriptionInfo.EventTypes)
                {
                    await _consumerChannel.ExchangeDeclareAsync(exchange: eventFullName, type: "direct");

                    await _consumerChannel.QueueDeclareAsync(queue: eventFullName,
                                             durable: true,
                                             exclusive: false,
                                             autoDelete: false,
                                             arguments: null);

                    await _consumerChannel.QueueBindAsync(
                        queue: eventFullName,
                        exchange: eventFullName,
                        routingKey: eventType.Name);

                    var consumer = new AsyncEventingBasicConsumer(_consumerChannel);

                    consumer.ReceivedAsync += OnMessageReceived;

                    await _consumerChannel.BasicConsumeAsync(
                        queue: eventFullName,
                        autoAck: false,
                        consumer: consumer);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error starting RabbitMQ connection");
            }
        },
        TaskCreationOptions.LongRunning);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public void Dispose() => _consumerChannel?.Dispose();

    public async Task PublishAsync(IntegrationEvent @event)
    {
        var eventType = @event.GetType();
        var exchangeName = eventType.FullName!;
        var routingKey = eventType.Name!;

        using var channel = await _rabbitMQConnection!.CreateChannelAsync() ?? throw new InvalidOperationException("RabbitMQ connection is not open");

        await channel.ExchangeDeclareAsync(exchange: exchangeName, type: "direct");

        var body = SerializeMessage(@event);

        await _pipeline.Execute(async () =>
        {
            try
            {
                await channel.BasicPublishAsync(
                        exchange: exchangeName,
                        routingKey: routingKey,
                        mandatory: true,
                        body: body);
            }
            catch (Exception ex)
            {
                logger.LogTrace("Exception occurred when publishing the event with Id: {EventId} in RabbitMQ, with message: {@exceptionMessage}", @event.Id, ex.Message);
                throw;
            }
        });
    }


    private async Task OnMessageReceived(object sender, BasicDeliverEventArgs eventArgs)
    {
        //Exchange name will match the event name
        var eventName = eventArgs.Exchange;
        var message = Encoding.UTF8.GetString(eventArgs.Body.Span);

        try
        {
            if (message.Contains("throw-fake-exception", StringComparison.InvariantCultureIgnoreCase))
                throw new InvalidOperationException($"Fake exception requested: \"{message}\"");

            await ProcessEvent(eventName, message);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error Processing message \"{Message}\"", message);
        }

        await _consumerChannel!.BasicAckAsync(eventArgs.DeliveryTag, multiple: false);
    }

    private async Task ProcessEvent(string eventName, string message)
    {
        await using var scope = serviceProvider.CreateAsyncScope();

        if (!_subscriptionInfo.EventTypes.TryGetValue(eventName, out var eventType))
        {
            logger.LogWarning("Unable to resolve event type for event name {EventName}", eventName);
            return;
        }

        var integrationEvent = DeserializeMessage(message, eventType);

        logger.LogInformation("Processing event {EventName} with Id {EventId}", eventName, integrationEvent.Id);

        foreach (var handler in scope.ServiceProvider.GetKeyedServices<IIntegrationEventHandler>(eventType))
            await handler.Handle(integrationEvent);
    }

    private IntegrationEvent DeserializeMessage(string message, Type eventType) =>
        JsonSerializer.Deserialize(message, eventType, _subscriptionInfo.JsonSerializerOptions) as IntegrationEvent
            ?? throw new Exception("Couldn't deserialize IntegrationEvent");

    private byte[] SerializeMessage(IntegrationEvent @event) =>
        JsonSerializer.SerializeToUtf8Bytes(@event, @event.GetType(), _subscriptionInfo.JsonSerializerOptions);

    private static ResiliencePipeline CreateResiliencePipeline(int retryCount)
    {
        var retryOptions = new RetryStrategyOptions
        {
            ShouldHandle = new PredicateBuilder().Handle<BrokerUnreachableException>().Handle<SocketException>(),
            MaxRetryAttempts = retryCount,
            DelayGenerator = (context) => ValueTask.FromResult(GenerateDelay(context.AttemptNumber))
        };

        return new ResiliencePipelineBuilder()
            .AddRetry(retryOptions)
            .Build();

        static TimeSpan? GenerateDelay(int attempt) => TimeSpan.FromSeconds(Math.Pow(2, attempt));
    }
}
