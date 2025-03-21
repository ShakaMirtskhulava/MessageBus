## **MessageBus Project**

This project defines the MessageBus abstraction.

At the core of this project is the **IEventBus** interface, which defines a basic `PublishAsync` method and an `IsReady` property. The `IsReady` property determines whether the connection to the message broker is established and messages can be published.

Additionally, this project defines the structure for events and event handlers using the **IntegrationEvent** record and the **IIntegrationEventHandler** interface.

We also have the **EventBusSubscriptionInfo** class, which is responsible for storing information about existing **integration events** and their respective **event handlers**. This allows us to configure specific brokers and specify the corresponding event handlers to be executed. The class also contains **JsonSerializerOptions**, which publishers and handlers use to serialize and deserialize events. These options can be configured. 

Another important property is `Dictionary<string, Type> EventTypes`, where the key is the event’s full name, and the value is its type.

To configure serialization options, we can use the **EventBusBuilderExtensions** class. If we don’t want to use the default serialization settings, we can override them here. Additionally, we can register subscriptions (events and their respective event handlers) using the `AddSubscription` method. This is where we leverage **Keyed Dependency Injection** to register the event handler class for a specific event type (key).

Finally, we have the **IEventBusBuilder**, which is essentially a wrapper for `IServiceCollection`.
