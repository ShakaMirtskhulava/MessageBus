namespace MessageBus.IntegrationEventLog.Publisher;

public class PublisherOptions
{
    public int DelayMs { get; set; }
    public int EventsBatchSize { get; set; }
    public int FailedMessageChainBatchSize { get; set; }
    public string EventTyepsAssemblyName { get; set; }

    public PublisherOptions(int delayMs, int eventsBatchSize, int failedMessageChainBatchSize, string eventTyepsAssemblyName = "")
    {
        DelayMs = delayMs;
        EventsBatchSize = eventsBatchSize;
        FailedMessageChainBatchSize = failedMessageChainBatchSize;
        EventTyepsAssemblyName = eventTyepsAssemblyName;
    }

    public PublisherOptions()
    {
        EventTyepsAssemblyName = string.Empty;
    }
}
