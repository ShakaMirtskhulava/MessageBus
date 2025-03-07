# MessageBus for Microservices

This repository contains both the abstraction and the implementation of the MessageBus for microservices. We're using **RabbitMQ** as the message broker. This implementation is suitable for microservices that have a dedicated **EventPublisher** and **EventHandler** project, as well as for any .NET app that wants to implement both publishing and handling logic within a single project.

## **MessageBus Project**

First of all, we have the **MessageBus** project. This project defines the MessageBus abstraction.

At the core of this project is the **IEventBus** interface, which defines a basic `PublishAsync` method and an `IsReady` property. The `IsReady` property determines whether the connection to the message broker is established and messages can be published.

Additionally, this project defines the structure for events and event handlers using the **IntegrationEvent** record and the **IIntegrationEventHandler** interface.

We also have the **EventBusSubscriptionInfo** class, which is responsible for storing information about existing **integration events** and their respective **event handlers**. This allows us to configure specific brokers and specify the corresponding event handlers to be executed. The class also contains **JsonSerializerOptions**, which publishers and handlers use to serialize and deserialize events. These options can be configured. 

Another important property is `Dictionary<string, Type> EventTypes`, where the key is the event’s full name, and the value is its type.

To configure serialization options, we can use the **EventBusBuilderExtensions** class. If we don’t want to use the default serialization settings, we can override them here. Additionally, we can register subscriptions (events and their respective event handlers) using the `AddSubscription` method. This is where we leverage **Keyed Dependency Injection** to register the event handler class for a specific event type (key).

Finally, we have the **IEventBusBuilder**, which is essentially a wrapper for `IServiceCollection`.

## **MessageBus.RabbitMQ Project**

The second project is **MessageBus.RabbitMQ**, which is a specific implementation of the EventBus using **RabbitMQ** as the message broker.

The main class here is **RabbitMQEventBus**, which implements both **IEventBus** and **IHostedService**. This means it serves as both an event bus and a background job. Its primary responsibility is to **establish a connection** to RabbitMQ as soon as it starts. It then declares exchanges, queues, and binds them for each **SubscriptionInfo**. Some projects may not require any subscriptions.

The **OnMessageReceived** method is responsible for registering event handlers for specific events retrieved from **SubscriptionInfo**. This method then calls **ProcessEvent**, which resolves the event handler from the DI container using the event’s name and registers it with the broker.

Another critical method is `PublishAsync`, which takes an `IntegrationEvent`. This method retrieves the **name** and **full name** of the event and looks for an exchange with the same name. It then publishes the event there using the event name as the **routing key**.

To ensure resiliency, events are published via **ResiliencePipeline**, which allows us to configure different **resiliency strategies**. Currently, we use a **Retry Strategy** that handles `BrokerUnreachableException` and `SocketException`, with the retry count configurable via **EventBusOptions** in `appsettings.json`.

To register **RabbitMQEventBus** in the DI container as both `IEventBus` and `IHostedService`, we use the **RabbitMqDependencyInjectionExtensions** class. The extension methods also require the consumer to configure the **ConnectionFactory** for the broker.

## **Additional Projects**

Apart from the core MessageBus implementation, we have three additional projects:

1. **MessageBus.Client** – A test project demonstrating how to implement both message publishing and handling within a single .NET app.
2. **MessageBus.EventHandler** – A project dedicated to handling events.
3. **MessageBus.EventPublisher** – A project dedicated to publishing events.

### **MessageBus.Client**

In **MessageBus.Client**, everything is straightforward. First, we register **RabbitMQ** as our message bus in `Program.cs`, configuring the connection factory. We then create subscriptions by registering **event handler classes** for specific events.

A test endpoint (`/home`) is responsible for creating an `IntegrationEvent` and publishing it to the broker. We also define **OrderCreatedEventHandler**, which executes as soon as the event is published in RabbitMQ. Since this project implements both publishing and subscribing logic, when the **EventBus job starts**, it immediately creates exchanges, queues, and bindings.

### **Segregated Event Publisher and Event Handler**

For projects that want to separate event publishing and handling, we use **MessageBus.EventHandler** and **MessageBus.EventPublisher**.

#### **Event Handler**

In **EventHandler**, we use `AddRabbitMqEventBus` and `AddSubscription` for each event. When the EventBus job starts, it detects **SubscriptionInfos** and creates respective queues, exchanges, and bindings.

It is **critical** to create all **AMQP (Advanced Message Queuing Protocol) components** from the **EventHandler project**, as it is the only project that knows about **event handlers**. The **EventPublisher** does not know about them—it only declares exchanges and publishes messages. Therefore, the **EventHandler project must run first**, followed by the **EventPublisher**. This ensures that once the publisher sends messages to the exchange, they are correctly routed to their respective queues and not lost.

#### **Event Publisher**

The **EventPublisher** project also uses `AddRabbitMqEventBus` to configure RabbitMQ EventBus. It registers a **Publisher background service**, responsible for reading events from the database in batches, updating their statuses to **InProgress** or **Published**, and publishing them to the EventBus.

A key concern here is ensuring that publishing starts **only after the connection to RabbitMQ is established**. We can determine when this happens by checking `eventBus.IsReady`. This also applies to the **single implementor model**, but in that case, event publishing is usually triggered by **user actions**, meaning the connection is likely already established before an event is published.

---

This is how the **MessageBus** is implemented and integrated with a specific message broker. If you have any questions, feel free to open an issue or contribute to the project!
