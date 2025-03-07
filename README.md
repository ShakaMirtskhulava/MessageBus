This repo contains the Abstraction and the implementation of the MessageBus of microservices.
We're using RabbitMQ as a message broker, this implementation will be suitable for the microservices that has the dedicated EventPublisher and Event Handler project,
as well as for any .net app that want to implement both publishing and handling logic in a single project.

First of all we have a **MessageBus** project. This project defines the MessageBus abstraction.
First of all we have an IEventBus interface there which defines 1 basic publish method as well as IsReady property, which is used to determine whether the connection
to the message broker is estabilished and the messages can be published.

There we also define what the events and event handlers should look like using the IntegrationEvent record and IIntegrationEventHandler interface. 
