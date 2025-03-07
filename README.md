This repo contains the Abstraction and the implementation of the MessageBus of microservices.
We're using RabbitMQ as a message broker, this implementation will be suitable for the microservices that has the dedicated EventPublisher and Event Handler project,
as well as for any .net app that want to implement both publishing and handling logic in a single project.

First of all we have a **MessageBus** project. This project defines the MessageBus abstraction.
First of all we have an IEventBus interface there which defines 1 basic publish method as well as IsReady property, which is used to determine whether the connection
to the message broker is estabilished and the messages can be published.

There we also define what the events and event handlers should look like using the IntegrationEvent record and IIntegrationEventHandler interface.
We alos have the EventBusSubscriptionInfo class which is responsible for storing the information about the existing integrationEvents and respective event handler, so that when we'll configure
specific brokers, we can specify the event handler that'll be executed. This class also contains JsonSerializerOptions that'll be used by the publishers and handlers to serialize and deserialize the events.
This options can be configured. This class also has Dictionary<string, Type> EventTypes, where the key is the fullname of the event and the value is the type of the evet.

We can use EventBusBuilderExtensions class to configure the JsonSerializationOptions(If we don't want to use the default version), here is we also registering the Subscribtions(events and their respective event handlers) using
AddSubscription method. Here we're using Keyed Dependency injection to register the eventHandler class for the specific event type(key).

We also have the IEventBusBuilder which is just a wrapper of thew IServiceCollection.



