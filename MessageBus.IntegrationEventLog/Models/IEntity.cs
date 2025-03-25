namespace MessageBus.IntegrationEventLog.Models;

public interface IEntity<TKey> where TKey : struct, IEquatable<TKey>
{
    public TKey Id { get; set; }
}