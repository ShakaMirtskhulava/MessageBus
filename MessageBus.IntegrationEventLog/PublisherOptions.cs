namespace MessageBus.IntegrationEventLog;

public record PublisherOptions(int delayMs, int eventsBatchSize, int failedMessageChainBatchSize, string eventTyepsAssemblyName = "");
